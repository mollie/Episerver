using EPiServer.Commerce.Order;

namespace Mollie.Checkout.ProcessCheckout.Interfaces
{
    public interface IProcessCheckout
    {
        PaymentProcessingResult Process(ICart cart, IPayment payment);
    }
}
