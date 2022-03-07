using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Mollie.Checkout.MollieApi;

namespace Foundation.Features.Api
{
    public class MollieApiController : Controller
    {
        private readonly HttpClient _httpClient;

        public MollieApiController(HttpClient httpClient)
        {
            _httpClient = new HttpClient();
        }

        public async Task<ActionResult> ValidateMerchant(string validationUrl)
        {
            var client = new MollieApplePayClient("live_jAe7MfwUsBEzqaKVyM7wVvy5aFA8md", _httpClient);
            var response = await client.ValidateMerchant(validationUrl);

            return Json(response);
        }
    }
}