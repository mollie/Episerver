using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer;
using EPiServer.Web.Routing;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Foundation.Commerce.Markets;

namespace Foundation.Features.Checkout.Payments
{
    public class MollieCheckoutPaymentOption : PaymentOptionBase
    {
        public override string SystemKeyword => "MollieCheckout";

        protected readonly LanguageService _languageService;

        public MollieCheckoutPaymentOption()
            : this(LocalizationService.Current, 
                  ServiceLocator.Current.GetInstance<IOrderGroupFactory>(), 
                  ServiceLocator.Current.GetInstance<ICurrentMarket>(), 
                  ServiceLocator.Current.GetInstance<LanguageService>(), 
                  ServiceLocator.Current.GetInstance<IPaymentService>())
        {
           
        }

        public MollieCheckoutPaymentOption(
            LocalizationService localizationService,
            IOrderGroupFactory orderGroupFactory,
            ICurrentMarket currentMarket,
            LanguageService languageService,
            IPaymentService paymentService)
           : base(localizationService, orderGroupFactory, currentMarket, languageService, paymentService)
        {
            _languageService = languageService;
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
    }
}