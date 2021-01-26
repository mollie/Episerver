# Episerver Mollie Checkout integration


## Intro

The Mollie.Checkout package helps with the implementation of [Mollie Checkout](https://docs.mollie.com/guides/checkout) 
(Mollie hosted payment pages) as payment method in episerver commerce. 


## Packages

[Mollie.Checkout] is the package for integration of Mollie Checkout in a Episerver commerce website.  
[Mollie.Checkout.CommerceManager] contains the usercontrol for configuration of the payment method in Episerver Commerce Manager.


## How it works / Flow

***Foundation flow


## Integration in Foundation 

<details><summary>1. Install Packages</summary>
<p>

Install package [Mollie.Checkout] in the __Foundation__ project and the __Foundation.CommerceManager__ project  
Install package [Mollie.Checkout.CommerceManager] in the __Foundation.CommerceManager__ project

</p>
</details>


<details><summary>2. Configure Payment in CommerceManager</summary>
<p>

In Episerver CommerceManager go to Administration >> Order System >> Payments >> _language_  
Click __New__ to add a new payment 

Fill (at least) the following fields:
#### On the Overview tab:_
- Name 
- System Keyword: Type __MollieCheckout__ 
- Language
- Class Name: Select __Mollie.Checkout.MollieCheckoutGateway__
- Payment class: Select __Mediachase.Commerce.Orders.OtherPayment__
- IsActive: Select __Yes__
#### On the Markets tab:
- Select markets to enable this paymentmethod for.

Click OK to Save, then open the payment again and navigate to the Parameters tab, and enter:

- Api Key: 
- Redirect URL: 

</p>
</details>

<details><summary>3. Create MollieCheckoutPaymentOption</summary>
<p>

In __Foundation\\Features\\Checkout\\Payments__ Add a new Class __MollieCheckoutPaymentOption.cs__

```csharp
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
        { }

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
``` 

</p>
</details>


<details><summary>4. Enable MollieCheckoutPaymentOption</summary>
<p>

In __Foundation\\Infrastructure\\InitializeSite.cs__ add

```csharp
   _services.AddTransient<IPaymentMethod, MollieCheckoutPaymentOption>();
```

</p>
</details>