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
        public string ActiveIssuer { get; set; }

        public override string SystemKeyword => "MollieCheckout";

        protected readonly LanguageService _languageService;
        protected readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentMethodsService _paymentMethodsService;
        private readonly ICartService _cartService;

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
                SubPaymentMethods = AsyncHelper.RunSync(() =>
                    _paymentMethodsService.LoadMethods(languageId, cart.GetTotal()));
            }
            else
            {
                SubPaymentMethods = AsyncHelper.RunSync(() =>
                    _paymentMethodsService.LoadMethods(languageId));
            }
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
            
            if (!string.IsNullOrWhiteSpace(SubPaymentMethod))
            {
                payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MolliePaymentMethod, SubPaymentMethod);

                if (SubPaymentMethod.Equals(Mollie.Checkout.Constants.MollieOrder.PaymentMethodIdeal, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrWhiteSpace(ActiveIssuer))
                {
                    payment.Properties.Add(Mollie.Checkout.Constants.OtherPaymentFields.MollieIssuer, ActiveIssuer);
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
    }
}