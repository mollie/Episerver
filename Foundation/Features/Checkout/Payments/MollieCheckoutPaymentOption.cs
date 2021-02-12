using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.GoogleAnalytics.Services.REST.Requests;
using EPiServer.ServiceLocation;
using Foundation.Commerce.Markets;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Mollie.Checkout.Models;
using Mollie.Checkout.Services;
using PaymentMethod = Mollie.Checkout.Models.PaymentMethod;

namespace Foundation.Features.Checkout.Payments
{
    public class MollieCheckoutPaymentOption : PaymentOptionBase
    {
        public override string SystemKeyword => "MollieCheckout";

        protected readonly LanguageService _languageService;
        protected readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        protected readonly IPaymentMethodsService _paymentMethodsService;



        public MollieCheckoutPaymentOption()
            : this(LocalizationService.Current,
                ServiceLocator.Current.GetInstance<IOrderGroupFactory>(),
                ServiceLocator.Current.GetInstance<ICurrentMarket>(),
                ServiceLocator.Current.GetInstance<LanguageService>(),
                ServiceLocator.Current.GetInstance<IPaymentService>(),
                ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>(),
                ServiceLocator.Current.GetInstance<IPaymentMethodsService>())
        {

        }

        public MollieCheckoutPaymentOption(
            LocalizationService localizationService,
            IOrderGroupFactory orderGroupFactory,
            ICurrentMarket currentMarket,
            LanguageService languageService,
            IPaymentService paymentService,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IPaymentMethodsService paymentMethodsService)
            : base(localizationService, orderGroupFactory, currentMarket, languageService, paymentService)
        {
            _languageService = languageService;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _paymentMethodsService = paymentMethodsService;

            //var languageId = _languageService.GetCurrentLanguage().Name;
            //_molliePaymentMethods = paymentMethodsService.LoadMethods(languageId).GetAwaiter().GetResult();

            //int x = _molliePaymentMethods.Count;
        }

        public override bool ValidateData() => true;

        public override IPayment CreatePayment(decimal amount, IOrderGroup orderGroup)
        {
            var languageId = _languageService.GetCurrentLanguage().Name;

            var payment = orderGroup.CreatePayment(OrderGroupFactory);

            payment.PaymentType = PaymentType.Other;
            payment.PaymentMethodId = PaymentMethodId;
            payment.PaymentMethodName = SystemKeyword;
            payment.Amount = amount;
            payment.Status = PaymentStatus.Pending.ToString();
            payment.TransactionType = TransactionType.Sale.ToString();

            payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.LanguageId, languageId);

            return payment;
        }


        public CheckoutConfiguration Configuration
        {
            get
            {
                var languageId = _languageService.GetCurrentLanguage().Name;
                return _checkoutConfigurationLoader.GetConfiguration(languageId);
            }
        }

        //public async Task<List<PaymentMethod>> MethodsAsync()
        //{
        //    var methods = await _paymentMethodsService.LoadMethods("en");

        //    return methods;
        //}



        //public List<PaymentMethod> Methods
        //{
        //    get
        //    {
        //        using (var client = new HttpClient())
        //        {
        //            var result = client.GetAsync("http://mollie.local/api/paymentmethods/get").Result;
        //            if (result.IsSuccessStatusCode)
        //            {
        //                var readTask = result.Content.ReadAsAsync<IList<PaymentMethod>>();
        //                readTask.Wait();

        //                return readTask.Result.ToList();
        //            }
        //        }

        //        return null;
        //    }
        //}

    }
}