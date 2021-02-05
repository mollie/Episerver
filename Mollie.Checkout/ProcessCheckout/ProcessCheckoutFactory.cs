using EPiServer.ServiceLocation;
using Mollie.Checkout.ProcessCheckout.Interfaces;
using Mollie.Checkout.Services;

namespace Mollie.Checkout.ProcessCheckout
{
    public static class ProcessCheckoutFactory
    {
        public static IProcessCheckout GetInstance(string languageId)
        {
            var checkoutConfigurationLoader = ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>();
            var checkoutConfiguration = checkoutConfigurationLoader.GetConfiguration(languageId);

            if (checkoutConfiguration.UseOrdersApi)
            {
                return new ProcessOrderCheckout();
            }

            return new ProcessPaymentCheckout();
        }
    }
}
