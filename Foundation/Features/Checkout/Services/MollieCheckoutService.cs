using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using System;
using System.Linq;
using static Mollie.Checkout.Constants;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IMollieCheckoutService))]
    public class MollieCheckoutService : IMollieCheckoutService
    {
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderRepository _orderRepository;

        public MollieCheckoutService(IOrderGroupCalculator orderGroupCalculator, IOrderRepository orderRepository)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _orderRepository = orderRepository;
        }

        public void HandlePaymentSuccess(IOrderGroup orderGroup, IPayment payment)
        {
            var cart = orderGroup as ICart;

            if (cart != null)
            {
                var processedPayments = orderGroup.GetFirstForm().Payments
                    .Where(x => x.Status.Equals(PaymentStatus.Processed.ToString()));

                var totalProcessedAmount = processedPayments.Sum(x => x.Amount);

                // If the Cart is completely paid
                if (totalProcessedAmount == orderGroup.GetTotal(_orderGroupCalculator).Amount)
                {
                    // Create order
                    var orderReference = (cart.Properties["IsUsePaymentPlan"] != null &&
                        cart.Properties["IsUsePaymentPlan"].Equals(true)) ?
                            SaveAsPaymentPlan(cart) :
                            _orderRepository.SaveAsPurchaseOrder(cart);

                    var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);

                    // Delete cart
                    _orderRepository.Delete(cart.OrderLink);

                    cart.AdjustInventoryOrRemoveLineItems((item, validationIssue) => { });
                }
            }
        }

        public void UpdateOrderStatus(IOrderGroup orderGroup, string mollieStatus)
        {
            if(orderGroup == null)
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

            if(string.IsNullOrEmpty(mollieStatus))
            {
                throw new ArgumentException(nameof(mollieStatus));
            }

            switch (mollieStatus)
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
                default:
                    break;
            }
        }

        public void HandlePaymentFailure(IOrderGroup orderGroup, IPayment payment)
        {
            // Do nothing, leave cart as is with failed payment.
        }

        private OrderReference SaveAsPaymentPlan(ICart cart)
        {
            throw new NotImplementedException("");
        }
    }
}
