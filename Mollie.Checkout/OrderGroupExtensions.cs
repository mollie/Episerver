using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;

namespace Mollie.Checkout
{
    public static class OrderGroupExtensions
    {
        public static string OrderNumber(this IOrderGroup orderGroup)
        {
            var orderNumberGenerator = ServiceLocator.Current.GetInstance<IOrderNumberGenerator>();

            if (string.IsNullOrWhiteSpace(orderGroup.Properties["OrderNumber"] as string))
            {
                // No orderNumber has been generated yet.
                orderGroup.Properties["OrderNumber"] = orderNumberGenerator.GenerateOrderNumber(orderGroup);
            }

            return orderGroup.Properties["OrderNumber"] as string;
        }
    }
}
