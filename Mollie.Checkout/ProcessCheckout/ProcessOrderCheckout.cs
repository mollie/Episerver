using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Commerce.Order;
using Mollie.Checkout.ProcessCheckout.Interfaces;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Services;
using System.Web;
using EPiServer.Security;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Security;
using Mollie.Api.Models.Order;
using Mollie.Checkout.ProcessCheckout.Helpers;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;
using Newtonsoft.Json;
using System.Net.Http;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Checkout.Services.Interfaces;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;
using System.Text;

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
        private readonly ServiceAccessor<HttpContextBase> _httpContextAccessor;
        private readonly CustomerContext _customerContext;
        private readonly IProductImageUrlFinder _productImageUrlFinder;
        private readonly IProductUrlGetter _productUrlGetter;
        private readonly HttpClient _httpClient;
        private readonly IOrderNoteHelper _orderNoteHelper;


        public ProcessOrderCheckout()
        {
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _paymentDescriptionGenerator = ServiceLocator.Current.GetInstance<IPaymentDescriptionGenerator>();
            _checkoutMetaDataFactory = ServiceLocator.Current.GetInstance<ICheckoutMetaDataFactory>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            _productImageUrlFinder = ServiceLocator.Current.GetInstance<IProductImageUrlFinder>();
            _productUrlGetter = ServiceLocator.Current.GetInstance<IProductUrlGetter>();
            _httpContextAccessor = ServiceLocator.Current.GetInstance<ServiceAccessor<HttpContextBase>>();
            _customerContext = CustomerContext.Current;
            _httpClient = ServiceLocator.Current.GetInstance<HttpClient>();
            _orderNoteHelper = ServiceLocator.Current.GetInstance<IOrderNoteHelper>();
        }

        public PaymentProcessingResult Process(ICart cart, IPayment payment)
        {
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            string selectedMethod = string.Empty;

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentMethod))
            {
                selectedMethod = payment.Properties[Constants.OtherPaymentFields.MolliePaymentMethod] as string;
            }
            
            var request = _httpContextAccessor().Request;
            
            var baseUrl = $"{request.Url?.Scheme}://{request.Url.Authority}";
            
            var urlBuilder = new UriBuilder(baseUrl)
            {
                Path = $"{Constants.Webhooks.MollieOrdersWebhookUrl}/{languageId}"
            };

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);
            var orderClient = new OrderClient(checkoutConfiguration.ApiKey, _httpClient);

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
                Amount = new Amount(cart.Currency.CurrencyCode, cart.GetTotal().Amount),
                Method = selectedMethod,
                BillingAddress = new OrderAddressDetails
                {
                    OrganizationName = billingAddress.Organization,
                    StreetAndNumber = $"{billingAddress.Line1} {billingAddress.Line2}",
                    City = billingAddress.City,
                    Region = billingAddress.RegionName,
                    PostalCode = billingAddress.PostalCode,
                    Country = billingAddress.CountryCode?.Length == 3 ? CountryCodeMapper.MapToTwoLetterIsoRegion(billingAddress.CountryCode) : billingAddress.CountryCode,
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
                    Country = shippingAddress.CountryCode?.Length == 3 ? CountryCodeMapper.MapToTwoLetterIsoRegion(shippingAddress.CountryCode) : shippingAddress.CountryCode,
                    GivenName = shippingAddress.FirstName,
                    FamilyName = shippingAddress.LastName,
                    Email = shippingAddress.Email,
                    Phone = shippingAddress.DaytimePhoneNumber
                },
                Metadata = JsonConvert.SerializeObject(metadata),
                ConsumerDateOfBirth = currentContact?.BirthDate,
                Locale = LanguageUtils.GetLocale(languageId),
                OrderNumber = orderNumber,
                RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={orderNumber}",
                WebhookUrl = urlBuilder.ToString(),
                Lines = GetOrderLines(cart)
            };

            var metaData = _checkoutMetaDataFactory.Create(cart, payment, checkoutConfiguration);

            orderRequest.SetMetadata(metaData);

            if (!string.IsNullOrWhiteSpace(selectedMethod) && selectedMethod.Equals(PaymentMethod.Ideal, StringComparison.InvariantCultureIgnoreCase))
            {
                if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MollieIssuer))
                {
                    var issuer = payment.Properties[Constants.OtherPaymentFields.MollieIssuer] as string;

                    orderRequest.Payment = new IDealSpecificParameters
                    {
                        Issuer = issuer
                    };
                }
            }

            OrderResponse createOrderResponse;

            try
            {
                createOrderResponse = orderClient.CreateOrderAsync(orderRequest).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.Error($"Creating Order in Mollie failed for Cart: {orderNumber}", e);

                throw new Exception($"Creating Order in Mollie failed for Cart: {orderNumber}");
            }

            OrderResponse getOrderResponse;

            try
            {
                getOrderResponse = orderClient.GetOrderAsync(createOrderResponse?.Id, true, false, false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.Error($"Getting Order from Mollie failed for Cart: {orderNumber}", e);

                throw new Exception($"Getting Order from Mollie failed for Cart: {orderNumber}");
            }

            var molliePaymentIdMessage = new StringBuilder();

            foreach (var molliePayment in getOrderResponse?.Embedded?.Payments)
            {
                if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentId))
                {
                    payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] = molliePayment.Id;

                    molliePaymentIdMessage.AppendLine($"Mollie Payment ID updated: {molliePayment?.Id}");
                }
                else
                {
                    payment.Properties.Add(Constants.OtherPaymentFields.MolliePaymentId, molliePayment.Id);

                    molliePaymentIdMessage.AppendLine($"Mollie Payment ID created: {molliePayment?.Id}");
                }
            }

            if (!string.IsNullOrWhiteSpace(molliePaymentIdMessage.ToString()))
            {
                _orderNoteHelper.AddNoteToOrder(cart, "Mollie Payment ID", molliePaymentIdMessage.ToString(), PrincipalInfo.CurrentPrincipal.GetContactId());
            }

            var message = $"Mollie Create Order is successful. Redirect end user to {getOrderResponse?.Links.Checkout.Href}";

            cart.Properties[Constants.PaymentLinkMollie] = getOrderResponse?.Links.Checkout.Href;

            _orderNoteHelper.AddNoteToOrder(cart, "Mollie Order created", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            _orderRepository.Save(cart);

            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message, getOrderResponse?.Links.Checkout.Href);
        }

        private IEnumerable<OrderLineRequest> GetOrderLines(
            IOrderGroup cart)
        {
            var shipment = cart.GetFirstShipment();
            var shippingAddress = shipment.ShippingAddress;
            var orderLines = cart.GetAllLineItems();
            var market = _marketService.GetMarket(cart.MarketId);
            var orderNumber = cart.OrderNumber();

            foreach (var orderLine in orderLines)
            {
                var metadata = new
                {
                    order_id = orderNumber,
                    line_code = orderLine.Code
                };

                //TODO: Check vat rate should be dependent om market
                var varRate = (orderLine.TaxCategoryId == null ? 0 : GetVatRate(orderLine.TaxCategoryId.Value)).ToString("0.00");
                var vatAmount = new Amount(cart.Currency.CurrencyCode, orderLine.GetSalesTax(market, cart.Currency, shippingAddress).Amount);

                yield return new OrderLineRequest
                {
                    Type = "physical",
                    Sku = orderLine.Code,
                    Name = orderLine.DisplayName,
                    ProductUrl = _productUrlGetter.Get(orderLine.GetEntryContent()),
                    ImageUrl = _productImageUrlFinder.Find(orderLine.GetEntryContent()),
                    Quantity = (int) orderLine.Quantity,
                    VatRate = vatAmount > 0 ? varRate : "0.00",
                    UnitPrice = new Api.Models.Amount(cart.Currency.CurrencyCode, orderLine.PlacedPrice),
                    TotalAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, orderLine.GetLineItemPrices(cart.Currency).DiscountedPrice),
                    DiscountAmount = new Api.Models.Amount(cart.Currency.CurrencyCode, orderLine.GetEntryDiscount()),
                    //TODO: Why is it returning 0 vat?
                    VatAmount = vatAmount,
                    Metadata = JsonConvert.SerializeObject(metadata)
                };
            }

            var shippingTotal = cart.GetShippingTotal().Amount;

            if (shippingTotal > 0)
            {
                var metadata = new
                {
                    order_id = orderNumber,
                    line_code = "shipment"
                };

                yield return new OrderLineRequest
                {
                    Type = "shipping_fee",
                    Name = "Shipping",
                    TotalAmount = new Amount(cart.Currency.CurrencyCode, shippingTotal),
                    UnitPrice = new Amount(cart.Currency.CurrencyCode, shippingTotal),
                    DiscountAmount = new Amount(cart.Currency.CurrencyCode, cart.GetShippingDiscountTotal().Amount),
                    Quantity = 1,
                    VatAmount = new Amount(cart.Currency.CurrencyCode, 0),
                    VatRate = "0",
                    Metadata = JsonConvert.SerializeObject(metadata)
                };
            }

            var orderDiscountTotal = cart.GetOrderDiscountTotal();

            if (orderDiscountTotal > 0)
            {
                yield return new OrderLineRequest
                {
                    Type = "discount",
                    Name = "Order Level Discount",
                    TotalAmount = new Amount(cart.Currency.CurrencyCode, -orderDiscountTotal.Amount),
                    UnitPrice = new Amount(cart.Currency.CurrencyCode, -orderDiscountTotal.Amount),
                    VatAmount = new Amount(cart.Currency.CurrencyCode, 0),
                    Quantity = 1,
                    VatRate = "0"
                };
            }
        }

        private static double GetVatRate(int taxCategoryId)
        {
            var taxDto = TaxManager.GetTax(taxCategoryId);
            var tax = taxDto?.TaxValue?.FirstOrDefault();

            return tax?.Percentage ?? 0;
        }
    }
}
