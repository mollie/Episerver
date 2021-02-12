using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        Task<List<Models.PaymentMethod>> LoadMethods(string languageId);


        List<Models.PaymentMethod> LoadMethodsSync(string languageId);

    }


    [ServiceConfiguration(typeof(IPaymentMethodsService))]
    public class PaymentMethodService : IPaymentMethodsService
    {
        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
       

        public PaymentMethodService(ICheckoutConfigurationLoader checkoutConfigurationLoader)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
        }

        public List<Models.PaymentMethod> LoadMethodsSync(string languageId)
        {
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);
            IPaymentMethodClient client = new Mollie.Api.Client.PaymentMethodClient(config.ApiKey);
            
            var locale = "nl-NL";

            // Get Payment Methods
            var result = client.GetPaymentMethodListAsync(locale: locale).GetAwaiter().GetResult();
            return result.Items.Select(x => new Models.PaymentMethod
            {
                Id = x.Id,
                Description = x.Description,
                ImageSize1x = x.Image?.Size1x,
                ImageSvg = x.Image?.Svg
            }).ToList();
        }


        public async Task<List<Models.PaymentMethod>> LoadMethods(string languageId)
        { 
            var config = _checkoutConfigurationLoader.GetConfiguration(languageId);

            IPaymentMethodClient client = new Mollie.Api.Client.PaymentMethodClient(config.ApiKey);

            var locale = "nl-NL";
            
            // Get Payment Methods
            var result = await client.GetPaymentMethodListAsync(locale: locale);
            return result.Items.Select(x => new Models.PaymentMethod
            {
                Id = x.Id,
                Description = x.Description,
                ImageSize1x = x.Image?.Size1x,
                ImageSvg =  x.Image?.Svg
            }).ToList();
        }
    }
}
