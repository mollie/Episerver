using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mollie.Api.Client;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.List;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.Dto;
using Mollie.Checkout.MollieApi;
using Mollie.Checkout.Helpers;
using Newtonsoft.Json;
using Currency = Mediachase.Commerce.Currency;

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

        public IEnumerable<KeyValuePair<string, string>> GetCurrencyValidationIssues(
            string locale, 
            IMarket market)
        {
            var culture = new CultureInfo(locale);
            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(culture.TwoLetterISOLanguageName);

            var enabledPaymentMethods = string.IsNullOrWhiteSpace(checkoutConfiguration.EnabledMolliePaymentMethods)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(checkoutConfiguration.EnabledMolliePaymentMethods);

            foreach (var currency in market.Currencies)
            {
                foreach (var enabledPaymentMethod in enabledPaymentMethods.Where(pm => pm.Locale == locale))
                {
                    var currencyPaymentMethods = GetPaymentMethods(
                        checkoutConfiguration.ApiKey,
                        market.DefaultLanguage.TextInfo.CultureName,
                        checkoutConfiguration.UseOrdersApi,
                        currency);

                    if (currencyPaymentMethods.All(cpm => cpm.Id != enabledPaymentMethod.Id))
                    {
                        yield return new KeyValuePair<string, string>(currency, enabledPaymentMethod.Description);
                    }
                }
            }
        }

        public IEnumerable<MolliePaymentMethod> GetPaymentMethods(
            string apiKey,
            string locale,
            bool useOrdersApi,
            Currency currency)
        {
            var httpClient = new HttpClient();
            var versionString = AssemblyVersionUtils.CreateVersionString();
            httpClient.DefaultRequestHeaders.Add("user-agent", versionString);

            var paymentMethodClient = new PaymentMethodClient(apiKey, httpClient);

            ListResponse<PaymentMethodResponse> paymentMethodResponses;

            try
            {
                paymentMethodResponses = AsyncHelper.RunSync(() => paymentMethodClient.GetPaymentMethodListAsync(
                    locale: locale,
                    resource: useOrdersApi ? Api.Models.Payment.Resource.Orders : Api.Models.Payment.Resource.Payments,
                    amount: currency == Currency.Empty ? null : new Amount(currency.CurrencyCode, 1000),
                    includeIssuers: false));
            }
            catch (MollieApiException)
            {
                paymentMethodResponses = new ListResponse<PaymentMethodResponse>
                {
                    Items = new EditableList<PaymentMethodResponse>()
                };
            }

            foreach (var paymentMethod in paymentMethodResponses.Items)
            {
                yield return new MolliePaymentMethod
                {
                    Id = paymentMethod.Id,
                    Description = paymentMethod.Description
                };
            }
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
