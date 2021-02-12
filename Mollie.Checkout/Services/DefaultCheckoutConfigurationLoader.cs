using EPiServer.ServiceLocation;
using Mollie.Checkout.Models;
using System;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Orders.Dto;

namespace Mollie.Checkout.Services
{
    [ServiceConfiguration(typeof(ICheckoutConfigurationLoader))]
    public class DefaultCheckoutConfigurationLoader : ICheckoutConfigurationLoader
    {
        public CheckoutConfiguration GetConfiguration(string languageId)
        {
            var paymentMethodDto = PaymentManager.GetPaymentMethodBySystemName(
                Constants.MollieCheckoutSystemKeyword, languageId, returnInactive: true);

            if (paymentMethodDto == null)
            {
                throw new Exception($"PaymentMethod {Constants.MollieCheckoutSystemKeyword} is not configured for language {languageId}");
            }

            return ReadConfiguration(paymentMethodDto);
        }

        private CheckoutConfiguration ReadConfiguration(PaymentMethodDto paymentMethodDto)
        {
            var useOrdersApi = false;

            if (bool.TryParse(paymentMethodDto.GetParameter(Constants.Fields.UseOrdersApiField)?.Value, out var useordersApiResult))
            {
                useOrdersApi = useordersApiResult;
            }

            return new CheckoutConfiguration
            {
                Environment = paymentMethodDto.GetParameter(Constants.Fields.EnvironmentField)?.Value ?? "test",
                ApiKey = paymentMethodDto.GetParameter(Constants.Fields.ApiKeyField)?.Value ?? string.Empty,
                ProfileId = paymentMethodDto.GetParameter(Constants.Fields.ProfileIDField)?.Value ?? string.Empty,
                RedirectUrl = paymentMethodDto.GetParameter(Constants.Fields.RedirectURLField)?.Value ?? string.Empty,
                VersionStrings = AssemblyVersionUtils.CreateVersionString(),
                UseOrdersApi = useOrdersApi
            };
        }
    }
}
