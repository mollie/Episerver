using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mollie.Api.Client;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.MollieApi;
using Mollie.Checkout.ProcessCheckout.Helpers;
using Mollie.Checkout.Helpers;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(IPaymentMethodsService))]
    public class PaymentMethodService : IPaymentMethodsService
    {
        private readonly IMolliePaymentMethodFilter _molliePaymentMethodFilter;
        private readonly IMolliePaymentMethodSorter _molliePaymentMethodSorter;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly HttpClient _httpClient;
        
        public PaymentMethodService(
            IMolliePaymentMethodFilter molliePaymentMethodFilter,
            IMolliePaymentMethodSorter molliePaymentMethodSorter,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            HttpClient httpClient)
        {
            _molliePaymentMethodFilter = molliePaymentMethodFilter;
            _molliePaymentMethodSorter = molliePaymentMethodSorter;
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

            var items = _molliePaymentMethodFilter.Filter(result.Items, languageId);
            items = _molliePaymentMethodSorter.Sort(items, languageId);

            return items.Select(MapToModel).ToList();
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(string languageId, Money cartTotal, string countryCode)
        {
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            // Get Payment Methods
            var client = new MolliePaymentMethodClient(config.ApiKey, _httpClient);

            var resource = config.UseOrdersApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var amount = new Api.Models.Amount(cartTotal.Currency.CurrencyCode, cartTotal.Amount);

            var billingCountry = countryCode?.Length == 3 ? CountryCodeMapper.MapToTwoLetterIsoRegion(countryCode) : countryCode;
            string locale = LanguageUtils.GetLocale(languageId, countryCode);

            var result = await client.GetPaymentMethodListAsync(locale: locale, resource: resource, amount: amount, includeIssuers: true/*, billingCountry: billingCountry*/);

            var items = _molliePaymentMethodFilter.Filter(result.Items, languageId);
            items = _molliePaymentMethodSorter.Sort(items, languageId);

            return items.Select(MapToModel).ToList();
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
