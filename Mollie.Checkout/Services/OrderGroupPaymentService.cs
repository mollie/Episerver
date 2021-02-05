using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mollie.Api.Models.Payment.Response;
using Newtonsoft.Json;
using System;
using static Mollie.Checkout.Constants;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IOrderGroupPaymentService))]
    public class OrderGroupPaymentService : IOrderGroupPaymentService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMollieCheckoutService _mollieCheckoutService;

        public OrderGroupPaymentService(
            IOrderRepository orderRepository,
            IMollieCheckoutService mollieCheckoutService)
        {
            _orderRepository = orderRepository;
            _mollieCheckoutService = mollieCheckoutService;
        }

        public void UpdateStatus(IOrderGroup orderGroup, IPayment orderGroupPayment, PaymentResponse paymentResponse)
        {
            if(orderGroup == null)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if (orderGroupPayment == null)
            {
                throw new ArgumentNullException(nameof(orderGroupPayment));
            }

            if (paymentResponse == null)
            {
                throw new ArgumentNullException(nameof(paymentResponse));
            }

            if (orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentId].ToString() == paymentResponse.Id)
            {
                orderGroupPayment.ProviderTransactionID = paymentResponse.Id;

                // Store Mollie Payment Status
                if (orderGroupPayment.Properties.ContainsKey(OtherPaymentFields.MolliePaymentStatus))
                {
                    orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentStatus] = paymentResponse.Status;
                }
                else
                {
                    orderGroupPayment.Properties.Add(OtherPaymentFields.MolliePaymentStatus, paymentResponse.Status);
                }

                orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentMethod] = paymentResponse.Method;
                orderGroupPayment.Properties[OtherPaymentFields.MolliePaymentFullResult] = JsonConvert.SerializeObject(paymentResponse);

                // Add Note to the Order
                OrderNoteHelper.AddNoteToOrder(orderGroup, "Mollie Payment Update",
                    $"--Mollie Payment Update received. New Status is {paymentResponse.Status}", Guid.Empty);

                switch (paymentResponse.Status)
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

                        _mollieCheckoutService.HandlePaymentSuccess(orderGroup, orderGroupPayment);
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
    }
}
