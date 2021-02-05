using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Order;
using Mollie.Checkout.ProcessCheckout.Interfaces;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Services;
using System.Web;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Security;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Security;
using Mollie.Api.Models.Order;
using Newtonsoft.Json;

namespace Mollie.Checkout.ProcessCheckout
{
    public class ProcessOrderCheckout : IProcessCheckout
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ProcessOrderCheckout));
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentDescriptionGenerator _paymentDescriptionGenerator;
        private readonly ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private readonly IOrderRepository _orderRepository;
        private readonly IMarketService _marketService;
        private readonly IRelationRepository _relationRepository;
        private readonly IContentLoader _contentLoader;
        private readonly UrlResolver _urlResolver;
        private readonly ServiceAccessor<HttpContextBase> _httpContextAccessor;
        private readonly CustomerContext _customerContext;

        public ProcessOrderCheckout()
        {
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _paymentDescriptionGenerator = ServiceLocator.Current.GetInstance<IPaymentDescriptionGenerator>();
            _checkoutMetaDataFactory = ServiceLocator.Current.GetInstance<ICheckoutMetaDataFactory>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            _relationRepository = ServiceLocator.Current.GetInstance<IRelationRepository>();
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
            _httpContextAccessor = ServiceLocator.Current.GetInstance<ServiceAccessor<HttpContextBase>>();
            _customerContext = CustomerContext.Current;
        }

        public PaymentProcessingResult Process(ICart cart, IPayment payment)
        {
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            var request = _httpContextAccessor().Request;
            var baseUrl = $"{request.Url?.Scheme}://{request.Url.Authority}";

            var urlBuilder = new UriBuilder(baseUrl)
            {
                Path = $"{Constants.Webhooks.MollieOrdersWebhookUrl}/{languageId}"
            };

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);
            var orderClient = new Api.Client.OrderClient(checkoutConfiguration.ApiKey);

            var shipment = cart.GetFirstShipment();
            var billingAddress = payment.BillingAddress;
            var shippingAddress = shipment.ShippingAddress;
            var orderNumber = cart.OrderNumber();
            var description = _paymentDescriptionGenerator.GetDescription(cart, payment);
            var currentContact = _customerContext.CurrentContact;

            var metadata = new
            {
                order_id = orderNumber,
                description
            };

            var orderRequest = new OrderRequest
            {
                Amount = new Api.Models.Amount(cart.Currency.CurrencyCode, payment.Amount),
                BillingAddress = new OrderAddressDetails
                {
                    OrganizationName = billingAddress.Organization,
                    StreetAndNumber = $"{billingAddress.Line1} {billingAddress.Line2}",
                    City = billingAddress.City,
                    Region = billingAddress.RegionName,
                    PostalCode = billingAddress.PostalCode,
                    Country = billingAddress.CountryCode,
                    GivenName = billingAddress.FirstName,
                    FamilyName = billingAddress.LastName,
                    Email = billingAddress.Email,
                    Phone = billingAddress.DaytimePhoneNumber
                },
                ShippingAddress = new OrderAddressDetails
                {
                    OrganizationName = shippingAddress.Organization,
                    StreetAndNumber = $"{shippingAddress.Line1} {shippingAddress.Line2}",
                    City = shippingAddress.City,
                    Region = shippingAddress.RegionName,
                    PostalCode = shippingAddress.PostalCode,
                    Country = shippingAddress.CountryCode,
                    GivenName = shippingAddress.FirstName,
                    FamilyName = shippingAddress.LastName,
                    Email = shippingAddress.Email,
                    Phone = shippingAddress.DaytimePhoneNumber
                },
                Metadata = JsonConvert.SerializeObject(metadata),
                ConsumerDateOfBirth = currentContact?.BirthDate,
                Locale = "nl_NL", //TODO: figure out locale
                OrderNumber = orderNumber,
                RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={orderNumber}",
                WebhookUrl = urlBuilder.ToString(),
                Lines = GetOrderLines(cart)
            };

            var temp = orderRequest.Lines.ToArray();

            var metaData = _checkoutMetaDataFactory.Create(cart, payment, checkoutConfiguration);

            orderRequest.SetMetadata(metaData);

            var orderResponse = orderClient.CreateOrderAsync(orderRequest).GetAwaiter().GetResult();

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentId))
            {
                payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] = orderResponse.Id;
            }
            else
            {
                payment.Properties.Add(Constants.OtherPaymentFields.MolliePaymentId, orderResponse.Id);
            }

            var message = $"--Mollie Create Order is successful. Redirect end user to {orderResponse.Links.Checkout.Href}";

            OrderNoteHelper.AddNoteToOrder(cart, "Mollie Order created", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            _orderRepository.Save(cart);

            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message, orderResponse.Links.Checkout.Href);
        }

        private IEnumerable<OrderLineRequest> GetOrderLines(
            IOrderGroup cart)
        {
            var shipment = cart.GetFirstShipment();
            var shippingAddress = shipment.ShippingAddress;
            var orderLines = cart.GetAllLineItems();
            var market = _marketService.GetMarket(cart.MarketId);
            var shipmentLines = shipment.LineItems;

            foreach (var orderLine in orderLines)
            {
                yield return new OrderLineRequest
                {
                    Type = "physical",
                    Sku = orderLine.Code,
                    Name = orderLine.DisplayName,
                    ProductUrl = GetUrl(orderLine.GetEntryContent()),
                    ImageUrl = GetImageUrl(orderLine.GetEntryContent()),
                    Quantity = (int) orderLine.Quantity,
                    VatRate = (orderLine.TaxCategoryId == null ? 0 : GetVatRate(orderLine.TaxCategoryId.Value)).ToString("0.00"),
                    UnitPrice = new Api.Models.Amount(cart.Currency.CurrencyCode, orderLine.PlacedPrice),
                    TotalAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, orderLine.GetLineItemPrices(cart.Currency).DiscountedPrice),
                    DiscountAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, orderLine.GetEntryDiscount()),
                    VatAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, orderLine.GetSalesTax(market, cart.Currency, shippingAddress).Amount)
                };
            }

            //foreach (var shipmentLine in shipmentLines)
            //{
            //    yield return new OrderLineRequest
            //    {
            //        Type = "shipping_fee",
            //        Sku = shipmentLine.Code,
            //        Name = shipmentLine.DisplayName,
            //        Quantity = (int)shipmentLine.Quantity,
            //        VatRate = (shipmentLine.TaxCategoryId == null ? 0 : GetVatRate(shipmentLine.TaxCategoryId.Value)).ToString("0.00"),
            //        UnitPrice = new Api.Models.Amount(cart.Currency.CurrencyCode, shipmentLine.PlacedPrice),
            //        TotalAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, shipmentLine.GetLineItemPrices(cart.Currency).DiscountedPrice),
            //        DiscountAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, shipmentLine.GetEntryDiscount()),
            //        VatAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, shipmentLine.GetSalesTax(market, cart.Currency, shippingAddress).Amount)
            //    };
            //}

            var orderDiscountTotal = cart.GetOrderDiscountTotal();
            if (orderDiscountTotal.Amount == 0)
            {
                yield break;
            }

            yield return new OrderLineRequest
            {
                Type = "discount",
                TotalAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, orderDiscountTotal.Amount)
            };
        }

        private string GetImageUrl(EntryContentBase entry)
        {
            if (!(entry is IAssetContainer assetContainer))
            {
                return SiteDefinition.Current.SiteUrl.ToString();
            }

            var productImageUrl = assetContainer.CommerceMediaCollection.Select(media =>
            {
                if (!_contentLoader.TryGet<IContentMedia>(media.AssetLink, out var contentMedia))
                {
                    return new KeyValuePair<string, string>(string.Empty, string.Empty);
                }

                var type = "Image";
                var url = _urlResolver.GetUrl(media.AssetLink, null, new VirtualPathArguments {ContextMode = ContextMode.Default});
                if (contentMedia is IContentVideo)
                {
                    type = "Video";
                }

                return new KeyValuePair<string, string>(type, url);

            }).FirstOrDefault(m => m.Key == "Image");

            if (string.IsNullOrWhiteSpace(productImageUrl.Value))
            {
                return SiteDefinition.Current.SiteUrl.ToString();
            }

            var absoluteUrl = new Uri(SiteDefinition.Current.SiteUrl, productImageUrl.Value);

            return absoluteUrl.ToString();
        }

        private string GetUrl(EntryContentBase entry)
        {
            var productLink = entry is VariationContent
                ? entry.GetParentProducts(_relationRepository).FirstOrDefault()
                : entry.ContentLink;

            if (productLink == null)
            {
                return string.Empty;
            }

            var urlBuilder = new UrlBuilder(_urlResolver.GetUrl(productLink));

            if (entry.Code != null && entry is VariationContent)
            {
                urlBuilder.QueryCollection.Add("variationCode", entry.Code);
            }

            var absoluteUrl = new Uri(SiteDefinition.Current.SiteUrl, urlBuilder.ToString());

            return absoluteUrl.ToString();
        }

        private static double GetVatRate(int taxCategoryId)
        {
            var taxDto = TaxManager.GetTax(taxCategoryId);
            var tax = taxDto?.TaxValue?.FirstOrDefault();

            return tax?.Percentage ?? 0;
        }
    }
}
