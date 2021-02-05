﻿using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mollie.Api.Client;
using Mollie.Api.Models.Payment.Response;
using Mollie.Checkout.Models;
using Mollie.Checkout.Services;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using static Mollie.Checkout.Constants;

namespace Mollie.Checkout.Webhooks
{
    [RoutePrefix(Constants.Webhooks.MolliePaymentsWebhookUrl)]
    public class MolliePaymentsWebhookApiController : ApiController
    {
        private readonly ILogger _log;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderRepository _orderRepository;
        private readonly IMollieCheckoutService _mollieCheckoutService;
        private readonly IOrderGroupPaymentService _orderGroupPaymentService;

        public MolliePaymentsWebhookApiController()
        {
            _log = LogManager.GetLogger(typeof(MolliePaymentsWebhookApiController));
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _mollieCheckoutService = ServiceLocator.Current.GetInstance<IMollieCheckoutService>();
            _orderGroupPaymentService = ServiceLocator.Current.GetInstance<IOrderGroupPaymentService>();
        }

        [HttpGet]
        [Route("isonline")]
        public string IsOnline()
        {
            return "Payments Webhook is Online!";
        }

        [HttpPost]
        [Route("{languageId}")]
        public async Task<IHttpActionResult> IndexAsync(string languageId)
        {
            var jsonResult = Request.Content.ReadAsStringAsync().Result;

            if(string.IsNullOrWhiteSpace(jsonResult))
            {
                _log.Error($"There is no Result from the Mollie Payments API.");

                return Ok();
            }

            var molliePaymentId = Regex.Match(jsonResult, @"id=(\w+)").Groups[1].Value;

            // Get Configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if(config == null)
            {
                _log.Error($"Configuration with LanguageID {languageId} has no config.");

                return Ok();
            }

            // Get Payment Client with API Key
            var paymentClient = new PaymentClient(config.ApiKey);

            // Get Payment from Mollie with API Key
            var paymentResponse = await paymentClient.GetPaymentAsync(molliePaymentId);
            
            if(paymentResponse == null)
            {
                _log.Error($"Mollie Payment with ID {molliePaymentId} has no result.");

                return Ok();
            }

            CheckoutMetaDataModel metaDataResponse = paymentResponse.GetMetadata<CheckoutMetaDataModel>();

            if (metaDataResponse == null)
            {
                _log.Error($"There is no Metadata available.");

                return Ok();
            }

            // Get Cart with ID
            var orderGroup = _orderRepository.Load<ICart>(metaDataResponse.CartId);

            if(orderGroup == null)
            {
                _log.Error($"Cart with ID {metaDataResponse.CartId} does not exist.");

                return Ok();
            }

            var orderGroupPayments = orderGroup.GetFirstForm().Payments;

            // Update Payments
            foreach (var orderGroupPayment in orderGroupPayments)
            {
                await HandlePaymentUpdateAsync(_orderGroupPaymentService, orderGroup, orderGroupPayment, paymentResponse);
            }

            return Ok();
        }

        private async Task HandlePaymentUpdateAsync(
            IOrderGroupPaymentService orderGroupPaymentService,
            IOrderGroup orderGroup,
            IPayment payment,
            PaymentResponse paymentResponse)
        {
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    orderGroupPaymentService.UpdateStatus(orderGroup, payment, paymentResponse);
                    return true;
                });
            }
            catch(Exception ex)
            {
                _log.Error("Error handling Payment Update", ex);
            }
        }
    }
}