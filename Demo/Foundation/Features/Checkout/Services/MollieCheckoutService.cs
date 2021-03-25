using EPiServer.Commerce.Order;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mollie.Checkout.Services.Interfaces;
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
        private readonly IOrderNoteHelper _orderNoteHelper;



        public MollieCheckoutService(IOrderGroupCalculator orderGroupCalculator, IOrderRepository orderRepository,
            IOrderNoteHelper orderNoteHelper)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _orderRepository = orderRepository;
            _orderNoteHelper = orderNoteHelper;
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

                    var message = "Converted to order by HandlePaymentSuccess initiated by webhook";
                    _orderNoteHelper.AddNoteToOrder(purchaseOrder, message, message, Guid.Empty);

                    purchaseOrder.Properties[MollieOrder.OrderIdMollie] = cart.Properties[MollieOrder.OrderIdMollie];
                    purchaseOrder.Properties[PaymentLinkMollie] = cart.Properties[PaymentLinkMollie];
                    purchaseOrder.Properties[MollieOrder.LanguageId] = payment.Properties[OtherPaymentFields.LanguageId];

                    _orderRepository.Save(purchaseOrder);

                    // Delete cart
                    _orderRepository.Delete(cart.OrderLink);

                    cart.AdjustInventoryOrRemoveLineItems((item, validationIssue) => { });
                }
            }
        }

        public void HandleOrderStatusUpdate(
            ICart cart, 
            string mollieStatus, 
            string mollieOrderId)
        {
            if(cart == null)
            {
                throw new ArgumentNullException(nameof(cart));
            }

            if(string.IsNullOrEmpty(mollieStatus))
            {
                throw new ArgumentException(nameof(mollieStatus));
            }

            if (string.IsNullOrEmpty(mollieOrderId))
            {
                throw new ArgumentException(nameof(mollieOrderId));
            }

            switch (mollieStatus)
            {
                case MollieOrderStatus.Created:
                case MollieOrderStatus.Pending:
                case MollieOrderStatus.Authorized:
                case MollieOrderStatus.Paid:
                case MollieOrderStatus.Shipping:
                    cart.OrderStatus = OrderStatus.InProgress;
                    break;
                case MollieOrderStatus.Completed:
                    cart.OrderStatus = OrderStatus.Completed;
                    break;
                case MollieOrderStatus.Canceled:
                case MollieOrderStatus.Expired:
                    cart.OrderStatus = OrderStatus.Cancelled;
                    break;
                default:
                    break;
            }

            cart.Properties[Constants.Cart.MollieOrderStatusField] = mollieStatus;
            cart.Properties[MollieOrder.OrderIdMollie] = mollieOrderId;

            _orderRepository.Save(cart);
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
