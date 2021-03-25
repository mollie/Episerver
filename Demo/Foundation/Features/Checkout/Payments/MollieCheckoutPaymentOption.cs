using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using Foundation.Commerce.Markets;
using Foundation.Features.Checkout.Services;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Mollie.Checkout.Helpers;
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
        private readonly IPaymentMethodsService _paymentMethodsService;
        private readonly ICartService _cartService;
        private readonly ICurrentMarket _currentMarket;

        private string _subPaymentMethodId;
        
        public MollieCheckoutPaymentOption()
            : this(LocalizationService.Current,
                ServiceLocator.Current.GetInstance<IOrderGroupFactory>(),
                ServiceLocator.Current.GetInstance<ICurrentMarket>(),
                ServiceLocator.Current.GetInstance<LanguageService>(),
                ServiceLocator.Current.GetInstance<IPaymentService>(),
                ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>(),
                ServiceLocator.Current.GetInstance<IPaymentMethodsService>(),
                ServiceLocator.Current.GetInstance<ICartService>())
        { }

        public MollieCheckoutPaymentOption(
            LocalizationService localizationService,
            IOrderGroupFactory orderGroupFactory,
            ICurrentMarket currentMarket,
            LanguageService languageService,
            IPaymentService paymentService,
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IPaymentMethodsService paymentMethodsService,
            ICartService cartService)
            : base(localizationService, orderGroupFactory, currentMarket, languageService, paymentService)
        {
            _languageService = languageService;
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _paymentMethodsService = paymentMethodsService;
            _cartService = cartService;
            _currentMarket = currentMarket;

            InitValues();
        }

        public IEnumerable<PaymentMethod> SubPaymentMethods { get; private set; }
        public CheckoutConfiguration Configuration { get; private set; }


        public void InitValues()
        {
            var languageId = _languageService.GetCurrentLanguage().Name;

            Configuration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var cart = _cartService.LoadCart(_cartService.DefaultCartName, false)?.Cart;

            if (cart != null)
            {
                var countryCode = GetCountryCode(cart);

                SubPaymentMethods = AsyncHelper.RunSync(() =>
                    _paymentMethodsService.LoadMethods(
                        cart.MarketId.Value,
                        languageId, 
                        cart.GetTotal(), 
                        countryCode));
            }
            else
            {
                SubPaymentMethods = AsyncHelper.RunSync(() =>
                    _paymentMethodsService.LoadMethods(
                        languageId));
            }
        }


        private string GetCountryCode(ICart cart)
        {
            if (cart.GetFirstForm().Payments.Any(p =>
                p.BillingAddress != null && !string.IsNullOrWhiteSpace(p.BillingAddress.CountryCode)))
            {
                return cart.GetFirstForm().Payments
                    .First(p => p.BillingAddress != null && !string.IsNullOrWhiteSpace(p.BillingAddress.CountryCode))
                    .BillingAddress.CountryCode;
            }

            if (cart.GetFirstForm().Shipments.Any(s =>
                s.ShippingAddress != null && !string.IsNullOrWhiteSpace(s.ShippingAddress.CountryCode)))
            {
                return cart.GetFirstForm().Shipments
                    .First(s => s.ShippingAddress != null && !string.IsNullOrWhiteSpace(s.ShippingAddress.CountryCode))
                    .ShippingAddress.CountryCode;
            }

            return _currentMarket.GetCurrentMarket().Countries.FirstOrDefault();
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

            if (!payment.Properties.ContainsKey(Mollie.Checkout.Constants.OtherPaymentFields.LanguageId))
            {
                payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.LanguageId, languageId);
            }
            else
            {
                payment.Properties[Mollie.Checkout.Constants.OtherPaymentFields.LanguageId] = languageId;
            }

            if (!string.IsNullOrWhiteSpace(SubPaymentMethod))
            {
                payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MolliePaymentMethod, SubPaymentMethod);

                if (SubPaymentMethod.Equals(Mollie.Checkout.Constants.MollieOrder.PaymentMethodIdeal, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(ActiveIssuer))
                {
                    payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MollieIssuer, ActiveIssuer);
                }

                if (SubPaymentMethod.Equals(Mollie.Checkout.Constants.MollieOrder.PaymentMethodCreditCard, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(CreditCardComponentToken))
                {
                    payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MollieToken, CreditCardComponentToken);
                }
            }

            return payment;
        }

        public string SubPaymentMethod 
        {
            get 
            {
                if (string.IsNullOrWhiteSpace(_subPaymentMethodId))
                {
                    var cartPayment = _cartService.LoadCart(_cartService.DefaultCartName, false)?.Cart?.GetFirstForm()?.Payments
                        .FirstOrDefault(p => p.PaymentMethodId == PaymentMethodId);

                    _subPaymentMethodId = cartPayment?.Properties[Mollie.Checkout.Constants.OtherPaymentFields.MolliePaymentMethod] as string;
                }
                return _subPaymentMethodId;
            }
            set => _subPaymentMethodId = value;
        }

        public string CreditCardComponentToken { get; set; }

        public string ActiveIssuer { get; set; }

        public string MollieDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SubPaymentMethod))
                {
                    return base.Description + " " + SubPaymentMethods.FirstOrDefault(x => x.Id.Equals(SubPaymentMethod,
                        StringComparison.InvariantCultureIgnoreCase))?.Description;
                }

                return base.Description;
            }
        }


        public string Locale => LanguageUtils.GetLocale(_languageService.GetCurrentLanguage().Name);
    }
}