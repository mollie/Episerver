using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mollie.Api.Client;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models.PaymentMethod;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IPaymentMethodsService))]
    public class PaymentMethodService : IPaymentMethodsService
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly HttpClient _httpClient;
        
        public PaymentMethodService(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            HttpClient httpClient)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _httpClient = httpClient;
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(string languageId)
        { 
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            string locale = LanguageUtils.GetLocale(languageId);

            // Get Payment Methods
            IPaymentMethodClient client = new PaymentMethodClient(config.ApiKey, _httpClient);

            var resource = config.UseOrdersApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var result = await client.GetPaymentMethodListAsync(locale: locale, resource: resource, includeIssuers: true);
            
            return result.Items.Select(MapToModel).ToList();
        }

        private Models.PaymentMethod MapToModel(PaymentMethodResponse response)
        {
            var methodModel = new Models.PaymentMethod
            {
                Id = response.Id,
                Description = response.Description,
                ImageSize1X = response.Image?.Size1x,
                ImageSize2X = response.Image?.Size2x,
                ImageSvg = response.Image?.Svg,
                Issuers = response.Issuers != null ?
                response.Issuers :
                null
            };
            
            if (response.MinimumAmount != null)
            {
                methodModel.MinimumAmount = new Money(response.MinimumAmount, response.MinimumAmount.Currency);
            }

            if (response.MaximumAmount != null)
            {
                methodModel.MaximumAmount = new Money(response.MaximumAmount, response.MaximumAmount.Currency);
            }

            return methodModel;
        }
    }
}
