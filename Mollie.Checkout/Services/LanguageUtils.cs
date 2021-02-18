using System.Globalization;

namespace Mollie.Checkout.Services
{
    public static class LanguageUtils
    {
        public static string GetLocale(string languageId)
        {
            var cultureInfo = new CultureInfo(languageId);

            return cultureInfo.TextInfo.CultureName;
        }
    }
}
