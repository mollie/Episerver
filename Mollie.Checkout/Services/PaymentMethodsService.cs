using System.Collections.Generic;
using System.Linq;
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
        
        public PaymentMethodService(ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(string languageId)
        { 
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            string locale = LanguageUtils.GetLocale(languageId);

            // Get Payment Methods
            IPaymentMethodClient client = new PaymentMethodClient(config.ApiKey);

            var resource = config.UseOrdersApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var result = await client.GetPaymentMethodListAsync(locale: locale, resource: resource);
            
            return result.Items.Select(MapToModel).ToList();
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(string languageId, Money cartTotal)
        {
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            // Get Payment Methods
            IPaymentMethodClient client = new PaymentMethodClient(config.ApiKey);

            string locale = LanguageUtils.GetLocale(languageId);
            
            var resource = config.UseOrdersApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var amount = new Api.Models.Amount(cartTotal.Currency.CurrencyCode, cartTotal.Amount);

            var result = await client.GetPaymentMethodListAsync(locale: locale, resource: resource, amount: amount);

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
                ImageSvg = response.Image?.Svg
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
