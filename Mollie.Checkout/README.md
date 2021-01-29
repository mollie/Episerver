# Episerver Mollie Checkout integration
<hr/>

## Intro

The Mollie.Checkout package helps with the implementation of [Mollie Checkout](https://docs.mollie.com/guides/checkout) 
(Mollie hosted payment pages) as payment method in episerver commerce. 


## Packages

[Mollie.Checkout] is the package for integration of Mollie Checkout in a Episerver commerce website.  
[Mollie.Checkout.CommerceManager] contains the usercontrol for configuration of the payment method in Episerver Commerce Manager.


## How it works / Flow

- Customer adds a product to the shoppingcart and navigates to the checkout page.
- On the checkout page the payment option 'Mollie Checkout' is available, and the customer selects this.
- The customer clicks on 'PLACE ORDER'
    - A payment is created using the Mollie Payments API (whitch returns a URL to redirect the customer to)
    - The customer is redirected to the Mollie Checkout page
- The customer completes the payment on Millie Checkout
- (Background) Updates on the payment are sent to a webhook by mollie
    - When the payment is successful an order is created for the cart.
- The customer is redirected to the 'Redirect page' specified in the mollie configuration.
    - This could be the order confirmation page
    - This also could be a page that waits for the payment to be processed, and redirects the user if a payment is received.
    

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


<details><summary>3. Create MollieCheckout Payment method</summary>
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

In __Foundation\\Features\\Checkout__ Add a new view ___MollieCheckoutPaymentMethod.cshtml__

```html

@model  Foundation.Features.Checkout.Payments.MollieCheckoutPaymentOption

@Html.HiddenFor(model => model.PaymentMethodId)

<br />
<div class="row">
    <div class="col-12">
        <div class="alert alert-info square-box">
            Mollie Payment method
        </div>
    </div>
</div>

```

In __Foundation\\Infrastructure\\InitializeSite.cs__ add

```csharp
   _services.AddTransient<IPaymentMethod, MollieCheckoutPaymentOption>();
```


</p>
</details>


<details><summary>4. Handle redirect to Mollie</summary>
<p>

After the processing of the pauments by Episerver, the mollie checkout payment will return a PaymentProcessingResult with IsSuccessful = true en een RedirectUrl.
In Foundation the user needs to be redirected to this Redirect url (url to the Mollie checkout page )

See the [CheckoutService.cs](https://dev.azure.com/arlanet/Mollie/_git/Mollie?path=%2FFoundation%2FFeatures%2FCheckout%2FServices%2FCheckoutService.cs) for an example of this on line 208

```csharp

    // Do we need a redirect to payment provider
    if (processPayments.Any(x => x.IsSuccessful && !string.IsNullOrWhiteSpace(x.RedirectUrl)))
    {
        var payment = processPayments.First(x => x.IsSuccessful && !string.IsNullOrWhiteSpace(x.RedirectUrl));
        HttpContext.Current.Response.Redirect(payment.RedirectUrl, true);
        return null;
    }

```

</p>
</details>


<details><summary>5. Implement IMollieCheckoutService</summary>
<p>

When a payment status update (paid, cancelled, etc..) is received from Mollie this service is called. 
Implement logic here to convert the cart to an order when the payment was successful.

See a sample implementation here:

```csharp

    [ServiceConfiguration(typeof(IMollieCheckoutService))]
    public class MollieCheckoutService : IMollieCheckoutService
    {
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderRepository _orderRepository;

        public MollieCheckoutService(IOrderGroupCalculator orderGroupCalculator, IOrderRepository orderRepository)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _orderRepository = orderRepository;
        }

        public void HandlePaymentSuccess(IOrderGroup orderGroup, IPayment payment)
        {
            var cart = orderGroup as ICart;

            if (cart != null)
            {
                var processedPayments = orderGroup.GetFirstForm().Payments
                    .Where(x => x.Status.Equals(PaymentStatus.Processed.ToString()));

                var totalProcessedAmount = processedPayments.Sum(x => x.Amount);

                // If the Cart is completely paid
                if (totalProcessedAmount == orderGroup.GetTotal(_orderGroupCalculator).Amount)
                {
                    // Create order
                    var orderReference = (cart.Properties["IsUsePaymentPlan"] != null &&
                        cart.Properties["IsUsePaymentPlan"].Equals(true)) ?
                            SaveAsPaymentPlan(cart) :
                            _orderRepository.SaveAsPurchaseOrder(cart);

                    var purchaseOrder = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);

                    // Delete cart
                    _orderRepository.Delete(cart.OrderLink);

                    cart.AdjustInventoryOrRemoveLineItems((item, validationIssue) => { });
                }
            }
        }

        public void HandlePaymentFailure(IOrderGroup orderGroup, IPayment payment)
        {
            // Do nothing, leave cart as is with failed payment.
        }

        private OrderReference SaveAsPaymentPlan(ICart cart)
        {
            throw new NotImplementedException("");
        }
    }

```

</p>
</details>


<details><summary>6. Change the Foundation Order-Confirmation controller</summary>
<p>

Change the Foundation Order-Confirmation page to accept the order trackingnumber instead of the order Id. \
See a sample of the changed OrderConfirmationController here:

```csharp

    public class OrderConfirmationController : OrderConfirmationControllerBase<OrderConfirmationPage>
    {
        private readonly ICampaignService _campaignService;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        public OrderConfirmationController(
            ICampaignService campaignService,
            ConfirmationService confirmationService,
            IAddressBookService addressBookService,
            IOrderGroupCalculator orderGroupCalculator,
            UrlResolver urlResolver, 
            ICustomerService customerService,
            IPurchaseOrderRepository purchaseOrderRepository) :
            base(confirmationService, addressBookService, orderGroupCalculator, urlResolver, customerService)
        {
            _campaignService = campaignService;
            _purchaseOrderRepository = purchaseOrderRepository;
        }
        public ActionResult Index(OrderConfirmationPage currentPage, string notificationMessage, string orderNumber)
        {
            IPurchaseOrder order = null;
            if (PageEditing.PageIsInEditMode)
            {
                order = _confirmationService.CreateFakePurchaseOrder();
            }
            else if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                if (int.TryParse(orderNumber, out int orderId))
                {
                    order = _confirmationService.GetOrder(orderId);
                }
                else
                {
                    order = _purchaseOrderRepository.Load(orderNumber);
                }
            }

            if (order != null && order.CustomerId == _customerService.CurrentContactId)
            {
                var viewModel = CreateViewModel(currentPage, order);
                viewModel.NotificationMessage = notificationMessage;

                _campaignService.UpdateLastOrderDate();
                _campaignService.UpdatePoint(decimal.ToInt16(viewModel.SubTotal.Amount));

                return View(viewModel);
            }

            return Redirect(Url.ContentUrl(ContentReference.StartPage));
        }
    }

```

</p>
</details>


<details><summary>7. Add a payment confirmation view</summary>
<p>
    
On the Foundation order-confirmation page a view is shown with some information about the payments for order.

Add a new view ____MollieCheckoutConfirmation.cshtml__ to __Foundation\\Features\\MyAccount\\OrderConfirmation

```html

@model EPiServer.Commerce.Order.IPayment 

<div>
    <h4>@Html.Translate("/OrderConfirmation/PaymentDetails")</h4>
    <p>
        @{ 
            var method = Model.Properties[Mollie.Checkout.Constants.OtherPaymentFields.MolliePaymentMethod] as string;
        }
        Paid by:  @(method ?? "Mollie Checkout")
        
    </p>
</div>

```

</p>
</details>