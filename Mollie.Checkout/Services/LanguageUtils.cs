using System.Globalization;
using System.Linq;

namespace Mollie.Checkout.Services
{
    public static class LanguageUtils
    {
        public static string GetLocale(string languageId)
        {
            var cultureInfo = new CultureInfo(languageId);

            return cultureInfo.TextInfo.CultureName;
        }

        public static string GetLocale(string languageId, string iso2CountryCode)
        {
            var validValues = new[]
            {
                "en-US", 
                "nl-NL", 
                "nl-BE",
                "fr_FR",
                "fr_BE",
                "de_DE", 
                "de_AT", 
                "de_CH", 
                "es_ES", 
                "ca_ES", 
                "pt_PT", 
                "it_IT", 
                "nb_NO", 
                "sv_SE", 
                "fi_FI", 
                "da_DK", 
                "is_IS", 
                "hu_HU", 
                "pl_PL", 
                "lv_LV", 
                "lt_LT"
            };

            if (validValues.Contains($"{languageId}-{iso2CountryCode.ToUpper()}"))
                return $"{languageId}-{iso2CountryCode.ToUpper()}";

            return GetLocale(languageId);
        }
    }
}
