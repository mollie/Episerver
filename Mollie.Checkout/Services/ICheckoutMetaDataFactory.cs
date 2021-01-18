using EPiServer.Commerce.Order;
using Mollie.Checkout.Models;

namespace Mollie.Checkout.Services
{
    public interface ICheckoutMetaDataFactory
    {
        string Create(IOrderGroup orderGroup, IPayment payment, CheckoutConfiguration configuration);
    }
}
