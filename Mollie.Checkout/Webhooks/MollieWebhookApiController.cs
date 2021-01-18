using System.Threading.Tasks;
using System.Web.Http;

namespace Mollie.Checkout.Webhooks
{
    [RoutePrefix("api/molliewebhook")]
    public class MollieWebhookApiController : ApiController
    {
        [HttpGet]
        [Route("isonline")]
        public string IsOnline()
        {
            return "Online!";
        }

        [HttpPost]
        [Route("")]
        public async Task<string> Index()
        {
            var json = Request.Content.ReadAsStringAsync().Result;

            return "[accepted]";
        }
    }
}
