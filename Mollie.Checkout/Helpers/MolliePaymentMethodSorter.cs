using System.Collections.Generic;
using System.Linq;
using Castle.Components.DictionaryAdapter;
using EPiServer.ServiceLocation;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.Dto;
using Mollie.Checkout.Services;
using Newtonsoft.Json;

namespace Mollie.Checkout.Helpers
{
    [ServiceConfiguration(typeof(IMolliePaymentMethodSorter))]
    public class MolliePaymentMethodSorter : IMolliePaymentMethodSorter
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;

        public MolliePaymentMethodSorter(
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public IEnumerable<PaymentMethodResponse> Sort(
            IEnumerable<PaymentMethodResponse> input,
            string languageId,
            string countryCode,
            string marketId)
        {
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var enabled = string.IsNullOrWhiteSpace(config.EnabledMolliePaymentMethods)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(config.EnabledMolliePaymentMethods);

            var enabledIds = enabled
                .Where(pm => pm.Country == countryCode && pm.MarketId == marketId && pm.OrderApi == config.UseOrdersApi)
                .Select(pm => pm.Id)
                .ToList();

            return input.OrderBy(pm => enabledIds.IndexOf(pm.Id));
        }
    }
}
