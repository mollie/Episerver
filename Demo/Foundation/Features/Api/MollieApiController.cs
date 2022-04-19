using System.Net.Http;
using System.Web.Mvc;
using Mollie.Checkout.MollieApi;
using Newtonsoft.Json;

namespace Foundation.Features.Api
{
    public class MollieApiController : Controller
    {
        private readonly HttpClient _httpClient;

        public MollieApiController(HttpClient httpClient)
        {
            _httpClient = new HttpClient();
        }

        public ActionResult ValidateMerchant(string validationUrl)
        {
            var client = new MollieApplePayClient("live_jAe7MfwUsBEzqaKVyM7wVvy5aFA8md", _httpClient);
            var response = client.ValidateMerchant(validationUrl);

            return Json(response, JsonRequestBehavior.AllowGet);
        }
    }
}