using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
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
    [RoutePrefix(Constants.Webhooks.MollieCheckoutWebhookUrl)]
    public class MollieCheckoutWebhookApiController : ApiController
    {
        private readonly ILogger _log = LogManager.GetLogger(typeof(MollieCheckoutWebhookApiController));

        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderRepository _orderRepository;
        private readonly IMollieCheckoutService _mollieCheckoutService;

        public MollieCheckoutWebhookApiController()
        {
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _mollieCheckoutService = ServiceLocator.Current.GetInstance<IMollieCheckoutService>();
        }

        [HttpGet]
        [Route("isonline")]
        public string IsOnline()
        {
            return "Webhook is Online!";
        }

        [HttpPost]
        [Route("{languageId}")]
        public async Task<IHttpActionResult> IndexAsync(string languageId)
        {
            var jsonResult = Request.Content.ReadAsStringAsync().Result;

            if(string.IsNullOrWhiteSpace(jsonResult))
            {
                _log.Error($"There is no Result from the Mollie API.");

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

            // Get Configuration with API Key
            var paymentClient = new PaymentClient(config.ApiKey);

            // Get Payment from Mollie with API Key
            var result = await paymentClient.GetPaymentAsync(molliePaymentId);
            
            if(result == null)
            {
                _log.Error($"Mollie Payment with ID {molliePaymentId} has no result.");

                return Ok();
            }

            CheckoutMetaDataModel metaData = new CheckoutMetaDataModel(result.Metadata);

            if (metaData == null)
            {
                _log.Error($"There is no Metadata available.");

                return Ok();
            }

            // Get Cart with ID
            var orderGroup = _orderRepository.Load<ICart>(metaData.OrderId);

            if(orderGroup == null)
            {
                _log.Error($"Cart with ID {metaData.OrderId} does not exist.");

                return Ok();
            }

            var orderGroupPayments = orderGroup.GetFirstForm().Payments;

            foreach (var orderGroupPayment in orderGroupPayments)
            {
                if (orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentId].ToString() == molliePaymentId)
                {
                    orderGroupPayment.ProviderTransactionID = molliePaymentId;

                    // Store Mollie Payment Status
                    if (orderGroupPayment.Properties.ContainsKey(OtherPaymentFields.MolliePaymentStatus))
                    {
                        orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentStatus] = result.Status;
                    }
                    else
                    {
                        orderGroupPayment.Properties.Add(OtherPaymentFields.MolliePaymentStatus, result.Status);
                    }

                    switch (result.Status)
                    {
                        case MolliePaymentStatus.Open:
                        case MolliePaymentStatus.Pending:
                        case MolliePaymentStatus.Authorized:
                            orderGroupPayment.Status = PaymentStatus.Pending.ToString();
                            _orderRepository.Save(orderGroup);
                            break;
                        case MolliePaymentStatus.Paid:
                            orderGroupPayment.Status = PaymentStatus.Processed.ToString();
                            _orderRepository.Save(orderGroup);

                            await HandlePaymentSuccessAsync(_mollieCheckoutService, orderGroup, orderGroupPayment);

                            break;
                        case MolliePaymentStatus.Canceled:
                        case MolliePaymentStatus.Expired:
                        case MolliePaymentStatus.Failed:
                            orderGroupPayment.Status = PaymentStatus.Failed.ToString();
                            _orderRepository.Save(orderGroup);

                            _mollieCheckoutService.HandlePaymentFailure(orderGroup, orderGroupPayment);
                            break;
                        default:
                            break;
                    }
                }
            }

            return Ok();
        }

        private async Task HandlePaymentSuccessAsync(IMollieCheckoutService checkoutService, IOrderGroup orderGroup, IPayment payment)
        {
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    checkoutService.HandlePaymentSuccess(orderGroup, payment);
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
