using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Mediachase.BusinessFoundation.Data.Business;
using Mollie.Api.Models.List;
using Mollie.Api.Models.PaymentMethod;

namespace Mollie.Checkout.ClientApi
{
    [RoutePrefix("api/paymentmethods")]
    public class PaymentMethodsApiController : ApiController
    {
        [HttpGet]
        [Route("get")]
        public async Task<List<Models.PaymentMethod>> Get()
        {
            var client = new Mollie.Api.Client.PaymentMethodClient("test_VBJcMe87FxQRqnQHU7WTBE2bdUKKFH");

            var result = await client.GetPaymentMethodListAsync();

            return result.Items.Select(x => new Models.PaymentMethod
            {
                Id = x.Id,
                Description = x.Description,
                ImageSize1x = x.Image?.Size1x,
                ImageSvg = x.Image?.Svg
            }).ToList();
        }
    }
}
