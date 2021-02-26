using System;
using System.Globalization;
using EPiServer.Commerce.Order;
using Mollie.Checkout.ProcessCheckout.Interfaces;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Security;
using Mollie.Checkout.Services;
using System.Web;
using System.Net.Http;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models;
using Mollie.Checkout.MollieClients.Interfaces;
using Mollie.Checkout.Services.Interfaces;
using Mollie.Api.Models.Payment;
using Mollie.Api.Models.Payment.Response;

namespace Mollie.Checkout.ProcessCheckout
{
    public class ProcessPaymentCheckout : IProcessCheckout
    {
        private readonly ILogger _logger;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentDescriptionGenerator _paymentDescriptionGenerator;
        private readonly ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private readonly IOrderRepository _orderRepository;
        private readonly ServiceAccessor<HttpContextBase> _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly IMolliePaymentClient _molliePaymentClient;
        private readonly IOrderNoteHelper _orderNoteHelper;

        public ProcessPaymentCheckout()
        {
            _logger = LogManager.GetLogger(typeof(ProcessPaymentCheckout));
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _paymentDescriptionGenerator = ServiceLocator.Current.GetInstance<IPaymentDescriptionGenerator>();
            _checkoutMetaDataFactory = ServiceLocator.Current.GetInstance<ICheckoutMetaDataFactory>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _httpContextAccessor = ServiceLocator.Current.GetInstance<ServiceAccessor<HttpContextBase>>();
            _httpClient = ServiceLocator.Current.GetInstance<HttpClient>();
            _molliePaymentClient = ServiceLocator.Current.GetInstance<IMolliePaymentClient>();
            _orderNoteHelper = ServiceLocator.Current.GetInstance<IOrderNoteHelper>();
        }

        public ProcessPaymentCheckout(
            ILogger logger,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IPaymentDescriptionGenerator paymentDescriptionGenerator,
            ICheckoutMetaDataFactory checkoutMetaDataFactory,
            IOrderRepository orderRepository,
            ServiceAccessor<HttpContextBase> httpContextAccessor,
            HttpClient httpClient,
            IMolliePaymentClient molliePaymentClient,
            IOrderNoteHelper orderNoteHelper)
        {
            _logger = logger;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _paymentDescriptionGenerator = paymentDescriptionGenerator;
            _checkoutMetaDataFactory = checkoutMetaDataFactory;
            _orderRepository = orderRepository;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
            _molliePaymentClient = molliePaymentClient;
            _orderNoteHelper = orderNoteHelper;
        }

        public PaymentProcessingResult Process(ICart cart, IPayment payment)
        {
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            if (string.IsNullOrWhiteSpace(languageId))
            {
                throw new CultureNotFoundException("Unable to get payment language.");
            }

            var request = _httpContextAccessor().Request;
            var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}";

            var urlBuilder = new UriBuilder(baseUrl)
            {
                Path = $"{Constants.Webhooks.MolliePaymentsWebhookUrl}/{languageId}"
            };

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if (string.IsNullOrWhiteSpace(checkoutConfiguration?.RedirectUrl))
            {
                throw new ApplicationException("Redirect url configuration not set.");
            }

            if (string.IsNullOrWhiteSpace(checkoutConfiguration.ApiKey))
            {
                throw new ApplicationException("Api key configuration not set.");
            }

            var paymentMethod = string.Empty;

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentMethod))
            {
                paymentMethod = payment.Properties[Constants.OtherPaymentFields.MolliePaymentMethod] as string;
            }

                


            var paymentRequest = new PaymentRequest
            {
                Amount = new Amount(cart.Currency.CurrencyCode, payment.Amount),
                Description = _paymentDescriptionGenerator.GetDescription(cart, payment),
                RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={cart.OrderNumber()}",
                WebhookUrl = urlBuilder.ToString(),
                Locale = LanguageUtils.GetLocale(languageId),
                Method = paymentMethod
            };

            if (!string.IsNullOrWhiteSpace(paymentMethod) && paymentMethod.Equals(PaymentMethod.Ideal, StringComparison.InvariantCultureIgnoreCase))
            {
                if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MollieIssuer))
                {
                    var issuer = payment.Properties[Constants.OtherPaymentFields.MollieIssuer] as string;

                    paymentRequest = new IdealPaymentRequest
                    {
                        Amount = paymentRequest.Amount,
                        Description = paymentRequest.Description,
                        RedirectUrl = paymentRequest.RedirectUrl,
                        WebhookUrl = paymentRequest.WebhookUrl,
                        Locale = paymentRequest.Locale,
                        Method = paymentRequest.Method,
                        Issuer = issuer
                    };
                }
            }


            if (!string.IsNullOrWhiteSpace(paymentMethod)
                && paymentMethod.Equals(PaymentMethod.CreditCard, StringComparison.InvariantCultureIgnoreCase)
                && !string.IsNullOrWhiteSpace(payment.Properties[Constants.OtherPaymentFields.MollieToken] as string))
            {
                var token = payment.Properties[Constants.OtherPaymentFields.MollieToken] as string;
                paymentRequest = new CreditCardPaymentRequest()
                {
                    Amount = new Amount(cart.Currency.CurrencyCode, payment.Amount),
                    Description = _paymentDescriptionGenerator.GetDescription(cart, payment),
                    RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={cart.OrderNumber()}",
                    WebhookUrl = urlBuilder.ToString(),
                    Locale = LanguageUtils.GetLocale(languageId),
                    CardToken = token
                };
            }

            var metaData = _checkoutMetaDataFactory.Create(cart, payment, checkoutConfiguration);

            paymentRequest.SetMetadata(metaData);

            PaymentResponse paymentResponse;

            try
            {
                paymentResponse = _molliePaymentClient.CreatePaymentAsync(paymentRequest, checkoutConfiguration.ApiKey, _httpClient).GetAwaiter().GetResult();

            }
            catch (Exception e)
            {
                _logger.Error($"Creating Payment in Mollie failed.", e);

                throw new ArgumentException($"Creating Payment in Mollie failed.");
            }

            string molliePaymentIdMessage;

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentId))
            {
                payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] = paymentResponse?.Id;

                molliePaymentIdMessage = $"Mollie Payment ID updated: {paymentResponse?.Id}";
            }
            else
            {
                payment.Properties.Add(Constants.OtherPaymentFields.MolliePaymentId, paymentResponse?.Id);

                molliePaymentIdMessage = $"Mollie Payment ID created: {paymentResponse?.Id}";
            }

            _orderNoteHelper.AddNoteToOrder(cart, "Mollie Payment ID", molliePaymentIdMessage, PrincipalInfo.CurrentPrincipal.GetContactId());

            var message = $"Mollie Create Payment is successful. Redirect end user to {paymentResponse?.Links.Checkout.Href}";

            _orderNoteHelper.AddNoteToOrder(cart, "Mollie Payment created", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            cart.Properties[Constants.PaymentLinkMollie] = paymentResponse.Links.Checkout.Href;

            _orderRepository.Save(cart);

            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message, paymentResponse?.Links.Checkout.Href);
        }
    }
}
