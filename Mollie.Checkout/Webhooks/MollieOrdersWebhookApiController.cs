﻿using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Mollie.Api.Client;
using Mollie.Api.Models.Payment.Response;
using Mollie.Checkout.Models;
using Mollie.Checkout.Services;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using static Mollie.Checkout.Constants;

namespace Mollie.Checkout.Webhooks
{
    [RoutePrefix(Constants.Webhooks.MollieOrdersWebhookUrl)]
    public class MollieOrdersWebhookApiController : ApiController
    {
        private readonly ILogger _log;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderGroupPaymentService _orderGroupPaymentService;

        public MollieOrdersWebhookApiController()
        {
            _log = LogManager.GetLogger(typeof(MollieOrdersWebhookApiController));
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _orderGroupPaymentService = ServiceLocator.Current.GetInstance<IOrderGroupPaymentService>();
        }

        [HttpGet]
        [Route("isonline")]
        public string IsOnline()
        {
            return "Orders Webhook is Online!";
        }

        [HttpPost]
        [Route("{languageId}")]
        public async Task<IHttpActionResult> IndexAsync(string languageId)
        {
            var jsonResult = Request.Content.ReadAsStringAsync().Result;

            if (string.IsNullOrWhiteSpace(jsonResult))
            {
                _log.Error($"There is no Result from the Mollie Orders API.");

                return Ok();
            }

            var mollieOrderId = Regex.Match(jsonResult, @"id=(\w+)").Groups[1].Value;

            // Get Configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if (config == null)
            {
                _log.Error($"Configuration with LanguageID {languageId} has no config.");

                return Ok();
            }

            // Get Order Client with API Key
            var orderClient = new OrderClient(config.ApiKey);

            // Get Order from Mollie with API Key
            var orderResult = await orderClient.GetOrderAsync(mollieOrderId);

            if (orderResult == null)
            {
                _log.Error($"Mollie Order with ID {mollieOrderId} has no result.");

                return Ok();
            }

            CheckoutMetaDataModel metaDataResponse = orderResult.GetMetadata<CheckoutMetaDataModel>();

            if (metaDataResponse == null)
            {
                _log.Error($"There is no Metadata available.");

                return Ok();
            }

            // Get Cart/Order with ID
            var orderGroup = _orderRepository.Load<ICart>(metaDataResponse.CartId);

            if (orderGroup == null)
            {
                _log.Error($"Cart with ID {metaDataResponse.CartId} does not exist.");

                return Ok();
            }

            // Update Cart/Order Status            
            switch(orderResult.Status)
            {
                case MollieOrderStatus.Created:
                case MollieOrderStatus.Pending:
                case MollieOrderStatus.Authorized:
                case MollieOrderStatus.Paid:
                case MollieOrderStatus.Shipping:
                    orderGroup.OrderStatus = OrderStatus.InProgress;
                    _orderRepository.Save(orderGroup);
                    break;
                case MollieOrderStatus.Completed:
                    orderGroup.OrderStatus = OrderStatus.Completed;
                    _orderRepository.Save(orderGroup);
                    break;
                case MollieOrderStatus.Canceled:
                case MollieOrderStatus.Expired:
                    orderGroup.OrderStatus = OrderStatus.Cancelled;
                    _orderRepository.Save(orderGroup);
                    break;
            }

            // Update Payments

            var orderGroupPayments = orderGroup.GetFirstForm().Payments;

            foreach (var orderGroupPayment in orderGroupPayments)
            {
                // await HandlePaymentUpdateAsync(_orderGroupPaymentService, orderGroupPayment, )
            }

            return Ok();
        }

        private async Task HandlePaymentUpdateAsync(
            IOrderGroupPaymentService orderGroupPaymentService,
            IOrderGroup orderGroup,
            IPayment payment,
            PaymentResponse paymentResponse,
            string molliePaymentId)
        {
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    orderGroupPaymentService.UpdateStatus(orderGroup, payment, paymentResponse, molliePaymentId);
                    return true;
                });
            }
            catch(Exception ex)
            {
                _log.Error("Error handling payment success", ex);
            }
        }
    }
}
