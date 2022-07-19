using System.Net.Http;
using System.Web.Mvc;
using Foundation.Commerce.Markets;
using Mollie.Checkout.MollieApi;
using Mollie.Checkout.Services;

namespace Foundation.Features.Api
{
    public class MollieApiController : Controller
    {
        private readonly LanguageService _languageService;
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;

        public MollieApiController(
            LanguageService languageService,
            ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _languageService = languageService;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public ActionResult ValidateMerchant(string validationUrl)
        {
            var languageId = _languageService.GetCurrentLanguage().Name;
            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            using (var httpClient = new HttpClient())
            {
                var client = new MollieApplePayClient(checkoutConfiguration.ApiKey, httpClient);
                var response = client.ValidateMerchant(validationUrl);

                return Json(response, JsonRequestBehavior.AllowGet);
            }
        }
    }
}