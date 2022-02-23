using System.Web.Mvc;

namespace Foundation.Features.MollieApplePay
{
    [AllowAnonymous]
    public class ApplePayController : Controller
    {
        [HttpGet]
        [Route("WellKnownApplePay")]
        public ActionResult WellKnownApplePay()
        {
            var associationFile = Server.MapPath("~/App_Data/apple-developer-merchantid-domain-association");

            return File(associationFile, "text/plain");
        }
    }
}