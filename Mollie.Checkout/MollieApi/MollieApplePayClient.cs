using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Mollie.Api.Client;
using Newtonsoft.Json;

namespace Mollie.Checkout.MollieApi
{
    public class MollieApplePayClient : BaseMollieClient
    {
        public MollieApplePayClient(string apiKey, HttpClient httpClient = null)
            : base(apiKey, httpClient)
        { }

        public async Task<ReponseValidateMerchant> ValidateMerchant(string validationUrl)
        {
            var data = new Dictionary<string, string>
            {
                { "validationUrl", validationUrl },
                { "domain", "5ba3-92-64-221-225.ngrok.io" }
            };

            var response = await this.PostAsync<ReponseValidateMerchant>("wallets/applepay/sessions", data)
                .ConfigureAwait(false);

            return response;
        }
    }

    public class ReponseValidateMerchant
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
