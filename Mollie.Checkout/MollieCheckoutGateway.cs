using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Plugins.Payment;
using System;

namespace Mollie.Checkout
{
    public class MollieCheckoutGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        /// <summary>
        /// Commerce 10 implementation
        /// </summary>
        public override bool ProcessPayment(Payment payment, ref string message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Commerce 11+ implementation
        /// </summary>
        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            throw new NotImplementedException();
        }
    }
}
