using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter.Xml;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mollie.Api.Client;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.List;
using Mollie.Api.Models.PaymentMethod;

namespace Mollie.Checkout.Services
{
    public interface IPaymentMethodsService
    {
        Task<bool> LoadPaymentMethods(IEnumerable<Models.PaymentMethod> paymentMethods, string languageId);

        Task<List<Models.PaymentMethod>> LoadMethods(string languageId);
    }


    [ServiceConfiguration(typeof(IPaymentMethodsService))]
    public class PaymentMethodService : IPaymentMethodsService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(PaymentMethodService));

        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
       

        public PaymentMethodService(ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public async Task<List<Models.PaymentMethod>> LoadMethods(string languageId)
        { 
            // Load configuration
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            // ToDo: Find a better way to map these languages.
            string locale;
            switch (languageId)
            {
                case "nl":
                    locale = "nl-NL";
                    break;
                default:
                    locale = "en-US";
                    break;
            }

            locale = "nl-NL";

            // Get Payment Methods
            IPaymentMethodClient client = new PaymentMethodClient(config.ApiKey);

            var resource = config.UseOrdersApi
                ? Api.Models.Payment.Resource.Orders
                : Api.Models.Payment.Resource.Payments;

            var result = await client.GetPaymentMethodListAsync(locale: locale, resource: resource);
            
            return result.Items.Select(MapToModel).ToList();
        }

        public async Task<bool> LoadPaymentMethods(IEnumerable<Models.PaymentMethod> paymentMethods, string languageId)
        {
            try
            {
                paymentMethods = await this.LoadMethods(languageId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return false;
            }
        }

        private Models.PaymentMethod MapToModel(PaymentMethodResponse response)
        {
            var methodModel = new Models.PaymentMethod
            {
                Id = response.Id,
                Description = response.Description,
                ImageSize1X = response.Image?.Size1x,
                ImageSize2X = response.Image?.Size2x,
                ImageSvg = response.Image?.Svg,
            };
            
            if (response.MinimumAmount != null)
            {
                methodModel.MinimumAmount = new Money(response.MinimumAmount, response.MinimumAmount.Currency);
            }

            if (response.MaximumAmount != null)
            {
                methodModel.MaximumAmount = new Money(response.MaximumAmount, response.MaximumAmount.Currency);
            }

            return methodModel;
        }
    }
}
