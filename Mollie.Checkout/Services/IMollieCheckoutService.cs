using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mollie.Checkout.Services
{
    public interface IMollieCheckoutService
    {
        void HandlePaymentSuccess(IOrderGroup orderGroup, IPayment payment);

        void HandlePaymentFailure(IOrderGroup orderGroup, IPayment payment);
    }

    
}
