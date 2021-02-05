﻿using System;
using EPiServer.Commerce.Order;
using Mollie.Checkout.ProcessCheckout.Interfaces;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Security;
using Mollie.Checkout.Services;
using System.Web;

namespace Mollie.Checkout.ProcessCheckout
{
    public class ProcessPaymentCheckout : IProcessCheckout
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ProcessPaymentCheckout));

        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentDescriptionGenerator _paymentDescriptionGenerator;
        private readonly ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private readonly IOrderRepository _orderRepository;
        private readonly ServiceAccessor<HttpContextBase> _httpContextAccessor;

        public ProcessPaymentCheckout()
        {
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _paymentDescriptionGenerator = ServiceLocator.Current.GetInstance<IPaymentDescriptionGenerator>();
            _checkoutMetaDataFactory = ServiceLocator.Current.GetInstance<ICheckoutMetaDataFactory>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _httpContextAccessor = ServiceLocator.Current.GetInstance<ServiceAccessor<HttpContextBase>>();
        }

        public PaymentProcessingResult Process(ICart cart, IPayment payment)
        {
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            var request = _httpContextAccessor().Request;
            var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}";

            var urlBuilder = new UriBuilder(baseUrl)
            {
                Path = $"{Constants.Webhooks.MolliePaymentsWebhookUrl}/{languageId}"
            };

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var paymentClient = new Api.Client.PaymentClient(checkoutConfiguration.ApiKey);

            var paymentRequest = new Api.Models.Payment.Request.PaymentRequest
            {
                Amount = new Api.Models.Amount(cart.Currency.CurrencyCode, payment.Amount),
                Description = _paymentDescriptionGenerator.GetDescription(cart, payment),
                RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={cart.OrderNumber()}",
                WebhookUrl = urlBuilder.ToString()
            };

            var metaData = _checkoutMetaDataFactory.Create(cart, payment, checkoutConfiguration);

            paymentRequest.SetMetadata(metaData);

            var paymentResponse = paymentClient.CreatePaymentAsync(paymentRequest).Result;

            if (payment.Properties.ContainsKey(Constants.OtherPaymentFields.MolliePaymentId))
            {
                payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] = paymentResponse.Id;
            }
            else
            {
                payment.Properties.Add(Constants.OtherPaymentFields.MolliePaymentId, paymentResponse.Id);
            }

            var message = $"--Mollie Create Payment is successful. Redirect end user to {paymentResponse.Links.Checkout.Href}";

            OrderNoteHelper.AddNoteToOrder(cart, "Mollie Payment created", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            _orderRepository.Save(cart);

            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message, paymentResponse.Links.Checkout.Href);
        }
    }
}