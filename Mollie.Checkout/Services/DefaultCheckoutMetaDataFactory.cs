using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mollie.Checkout.Models;
using Newtonsoft.Json;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(ICheckoutMetaDataFactory))]
    public class DefaultCheckoutMetaDataFactory : ICheckoutMetaDataFactory
    {
        public string Create(IOrderGroup orderGroup, IPayment payment, CheckoutConfiguration configuration)
        {
            var metaDataObject = new CheckoutMetaDataModel()
            {
                CartId = orderGroup.OrderLink.OrderGroupId,
                OrderNumber = orderGroup.OrderNumber(),
                Versions = configuration.VersionStrings
            };

            return JsonConvert.SerializeObject(metaDataObject);
        }
    }
}
