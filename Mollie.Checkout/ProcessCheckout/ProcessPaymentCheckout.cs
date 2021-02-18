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
using Mollie.Checkout.Helpers;

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

            var paymentRequest = new PaymentRequest
            {
                Amount = new Amount(cart.Currency.CurrencyCode, payment.Amount),
                Description = _paymentDescriptionGenerator.GetDescription(cart, payment),
                RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={cart.OrderNumber()}",
                WebhookUrl = urlBuilder.ToString(),
                Locale = LanguageUtils.GetLocale(languageId)
            };

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentMethod))
            {
                paymentRequest.Method = payment.Properties[Constants.OtherPaymentFields.MolliePaymentMethod] as string;
            }

            var metaData = _checkoutMetaDataFactory.Create(cart, payment, checkoutConfiguration);

            paymentRequest.SetMetadata(metaData);

            var paymentResponse = _molliePaymentClient.CreatePaymentAsync(paymentRequest, checkoutConfiguration.ApiKey, _httpClient)
                .GetAwaiter().GetResult();

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentId))
            {
                payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] = paymentResponse.Id;
            }
            else
            {
                payment.Properties.Add(Constants.OtherPaymentFields.MolliePaymentId, paymentResponse.Id);
            }

            var message = $"--Mollie Create Payment is successful. Redirect end user to {paymentResponse.Links.Checkout.Href}";

            _orderNoteHelper.AddNoteToOrder(cart, "Mollie Payment created", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            _orderRepository.Save(cart);

            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message, paymentResponse.Links.Checkout.Href);
        }
    }
}
