using EPiServer.ServiceLocation;
using Mollie.Checkout.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                throw new Exception($"PaymentMethod {Constants.MollieCheckoutSystemKeyword} is not configiured for language {languageId}");
            }

            return ReadConfiguration(paymentMethodDto);
        }

        private CheckoutConfiguration ReadConfiguration(PaymentMethodDto paymentMethodDto)
        {
            return new CheckoutConfiguration
            {
                ApiKey = paymentMethodDto.GetParameter(Constants.Fields.ApiKeyField)?.Value ?? string.Empty,
                ProfileId = paymentMethodDto.GetParameter(Constants.Fields.ProfileIDField)?.Value ?? string.Empty
            };
        }
    }
}
