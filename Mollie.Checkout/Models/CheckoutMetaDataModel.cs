using Newtonsoft.Json.Linq;

namespace Mollie.Checkout.Models
{
    public class CheckoutMetaDataModel
    {
        public int CartId { get; set; }

        public string OrderNumber { get; set; }

        public string VersionStrings { get; set; }
    }
}
