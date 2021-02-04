namespace Mollie.Checkout
{
    public static class Constants
    {
        public const string MollieCheckoutSystemKeyword = "MollieCheckout";

        public static class Fields
        {
            public const string EnvironmentField = "Environment";

            public const string ApiKeyField = "ApiKey";

            public const string ProfileIDField = "ProfileID";

            public const string RedirectURLField = "RedirectURL";

            public const string UseOrdersApiField = "UseOrdersApi";
        }

        public static class OtherPaymentFields
        {
            public const string LanguageId = "LanguageId";

            public const string MolliePaymentId = "MolliePaymentId";

            public const string MolliePaymentStatus = "MolliePaymentStatus";

            public const string MolliePaymentMethod = "MolliePaymentMethod";

            public const string MolliePaymentFullResult = "MolliePaymentFullResult";
        }

        public static class MolliePaymentStatus
        {
            public const string Open = "open";

            public const string Paid = "paid";

            public const string Pending = "pending";

            public const string Authorized = "authorized";

            public const string Canceled = "canceled";

            public const string Expired = "expired";

            public const string Failed = "failed";
        }

        public static class Webhooks
        {
            public const string MollieCheckoutWebhookUrl = "api/molliecheckoutwebhook";
        }
    }
}
