using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mollie.Api.Client;
using Mollie.Api.Models.Payment.Response;
using Mollie.Checkout.Models;
using Mollie.Checkout.Services;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using static Mollie.Checkout.Constants;

namespace Mollie.Checkout.Webhooks
{
    [RoutePrefix("api/molliewebhook")]
    public class MollieWebhookApiController : ApiController
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IOrderRepository _orderRepository;
        private readonly IMollieCheckoutService _mollieCheckoutService;

        public MollieWebhookApiController()
        {
            _checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            _mollieCheckoutService = ServiceLocator.Current.GetInstance<IMollieCheckoutService>();
        }

        [HttpGet]
        [Route("isonline")]
        public string IsOnline()
        {
            return "Online!";
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> IndexAsync(string languageId)
        {
            var jsonResult = Request.Content.ReadAsStringAsync().Result;

            if(string.IsNullOrWhiteSpace(jsonResult))
            {
                return Ok();
            }

            var molliePaymentId = Regex.Match(jsonResult, @"id=(\w+)").Groups[1].Value;

            // Get Configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            if(config == null)
            {
                return Ok();
            }

            // Get Configuration with API Key
            var paymentClient = new PaymentClient(config.ApiKey);

            // Get Payment from Mollie with API Key
            var result = await paymentClient.GetPaymentAsync(molliePaymentId);
            
            if(result == null)
            {
                return Ok();
            }

            CheckoutMetaDataModel metaData = new CheckoutMetaDataModel(result.Metadata);

            if (metaData == null)
            {
                return Ok();
            }

            // Get Cart with ID
            var orderGroup = _orderRepository.Load<ICart>(metaData.OrderId);

            if(orderGroup == null)
            {
                return Ok();
            }

            var orderGroupPayments = orderGroup.GetFirstForm().Payments;

            foreach (var orderGroupPayment in orderGroupPayments)
            {
                if (orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentId].ToString() == molliePaymentId)
                {
                    orderGroupPayment.TransactionID = molliePaymentId;

                    switch (result.Status)
                    {
                        case MolliePaymentStatus.Open:
                            orderGroupPayment.Status = MolliePaymentStatus.Open;
                            _orderRepository.Save(orderGroup);
                            break;
                        case MolliePaymentStatus.Paid:
                            orderGroupPayment.Status = PaymentStatus.Processed.ToString();
                            _orderRepository.Save(orderGroup);
                            _mollieCheckoutService.HandlePaymentSuccess(orderGroup, orderGroupPayment);
                            break;
                        case MolliePaymentStatus.Pending:
                            orderGroupPayment.Status = PaymentStatus.Pending.ToString();
                            _orderRepository.Save(orderGroup);
                            break;
                        case MolliePaymentStatus.Authorized:
                            orderGroupPayment.Status = MolliePaymentStatus.Authorized;
                            _orderRepository.Save(orderGroup);
                            break;
                        case MolliePaymentStatus.Canceled:
                            orderGroupPayment.Status = MolliePaymentStatus.Canceled;
                            _orderRepository.Save(orderGroup);
                            break;
                        case MolliePaymentStatus.Expired:
                            orderGroupPayment.Status = MolliePaymentStatus.Expired;
                            _orderRepository.Save(orderGroup);
                            break;
                        case MolliePaymentStatus.Failed:
                            orderGroupPayment.Status = PaymentStatus.Failed.ToString();
                            _orderRepository.Save(orderGroup);
                            _mollieCheckoutService.HandlePaymentFailure(orderGroup, orderGroupPayment);
                            break;
                        default:
                            break;
                    };
                }
            }

            return Ok();
        }
    }
}
