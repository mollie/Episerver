
namespace Mollie.Checkout.Dto
{
    public class MolliePaymentMethod
    {
        public string Id { get; set; }
        public string Locale { get; set; }
        public string Description { get; set; }
        public int Rank { get; set; }
    }
}