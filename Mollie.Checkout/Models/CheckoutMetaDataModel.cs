using Newtonsoft.Json.Linq;

namespace Mollie.Checkout.Models
{
    public class CheckoutMetaDataModel
    {
        public CheckoutMetaDataModel()
        { }

        public CheckoutMetaDataModel(string json)
        {
            var jObject = JObject.Parse(json);

            OrderId = (int)jObject["OrderId"];
            OrderNumber = (string)jObject["OrderNumber"];
            Versions = (string)jObject["Versions"];
        }

        public int OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string Versions { get; set; }
    }
}
