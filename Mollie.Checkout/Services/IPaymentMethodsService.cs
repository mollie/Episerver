using System.Collections.Generic;
using System.Threading.Tasks;
using Mediachase.Commerce;
using Mollie.Checkout.Dto;

namespace Mollie.Checkout.Services
{
    public interface IPaymentMethodsService
    {
        Task<List<Models.PaymentMethod>> LoadMethods(string languageId);

        Task<List<Models.PaymentMethod>> LoadMethods(string languageId, Money cartTotal, string countryCode);

        IEnumerable<KeyValuePair<string, string>> GetCurrencyValidationIssues(
            string locale,
            IMarket market);

        IEnumerable<MolliePaymentMethod> GetPaymentMethods(
            string apiKey,
            string locale,
            bool useOrdersApi,
            Currency currency);
    }
}
