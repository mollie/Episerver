namespace Mollie.Checkout.Models
{
    public class CheckoutConfiguration
    {
        public string Environment { get; set; }

        public string ApiKey { get; set; }

        public string ProfileId { get; set; }

        public string VersionStrings { get; set; }

        public string RedirectUrl { get; set; }
    }
}
