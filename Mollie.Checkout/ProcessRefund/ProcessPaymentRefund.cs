using System.Linq;
using System.Net.Http;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Security;
using Mediachase.MetaDataPlus;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Refund;
using Mollie.Checkout.Helpers;
using Mollie.Checkout.ProcessCheckout;
using Mollie.Checkout.ProcessRefund.Interfaces;
using Mollie.Checkout.Services;

namespace Mollie.Checkout.ProcessRefund
{
    [ServiceConfiguration(typeof(IProcessPaymentRefund))]
    public class ProcessPaymentRefund : IProcessPaymentRefund
    {
        private readonly IOrderRepository _orderRepository;
        private readonly HttpClient _httpClient;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ProcessOrderCheckout));

        public ProcessPaymentRefund(
            IOrderRepository orderRepository,
            HttpClient httpClient,
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _orderRepository = orderRepository;
            _httpClient = httpClient;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public PaymentProcessingResult Process(IOrderGroup orderGroup, IPayment payment)
        {
            if (!(orderGroup is IPurchaseOrder purchaseOrder))
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult("--Mollie Refund Payment is not successful. Order is not of expected type IPurchaseOrder.");
            }

            if (!(payment is Payment refundPayment))
            {
                return PaymentProcessingResult.CreateUnsuccessfulResult("--Mollie Refund Payment is not successful. Payment is not of expected type Payment.");
            }

            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);
            var refundClient = new RefundClient(checkoutConfiguration.ApiKey, _httpClient);
            var paymentId = payment.Properties[Constants.OtherPaymentFields.MolliePaymentId] as string;

            //TODO:Find better way to find current return form
            var returnForm = purchaseOrder.ReturnForms.FirstOrDefault(rf => ((OrderForm)rf).ObjectState == MetaObjectState.Modified);

            var refundResponse = refundClient.CreateRefundAsync(
                paymentId,
                new RefundRequest
                {
                    Amount = new Amount(orderGroup.Currency.CurrencyCode, refundPayment.Amount),
                    Description = returnForm?.ReturnComment ?? "Not set"
                }).GetAwaiter().GetResult();

            var message = $"--Mollie Refund Payment is successful. Refunded {refundResponse.Amount}, status {refundResponse.Status}.";

            OrderNoteHelper.AddNoteToOrder(orderGroup, "Mollie Payment refund", message, PrincipalInfo.CurrentPrincipal.GetContactId());

            _orderRepository.Save(orderGroup);
            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message);
        }
    }
}
