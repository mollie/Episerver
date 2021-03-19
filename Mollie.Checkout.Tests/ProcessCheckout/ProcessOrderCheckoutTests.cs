﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Storage;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using FakeItEasy;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Url;
using Mollie.Checkout.Helpers;
using Mollie.Checkout.Models;
using Mollie.Checkout.MollieClients.Interfaces;
using Mollie.Checkout.ProcessCheckout;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;
using Mollie.Checkout.Services;
using Mollie.Checkout.Services.Interfaces;

namespace Mollie.Checkout.Tests.ProcessCheckout
{
    [TestClass]
    public class ProcessOrderCheckoutTests
    {
        private ILogger _logger;
        private ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private IPaymentDescriptionGenerator _paymentDescriptionGenerator;
        private ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private IOrderRepository _orderRepository;
        private IMarketService _marketService;
        private ServiceAccessor<HttpContextBase> _httpContextAccessor;
        private IProductImageUrlFinder _productImageUrlFinder;
        private IProductUrlGetter _productUrlGetter;
        private HttpClient _httpClient;
        private IOrderNoteHelper _orderNoteHelper;
        private IMollieOrderClient _mollieOrderClient;
        private ICurrentCustomerContactGetter _currentCustomerContactGetter;
        private ProcessOrderCheckout _processOrderCheckout;

        private ICart _cart;
        private IPayment _payment;

        private const decimal Amount = 10;
        private const string CurrencyCode = "EUR";
        private const string PaymentDescription = "Payment Description";
        private const string RedirectUrl = "https://www.mollie.com/";
        private const string WebShopUrl = "https://www.webshop.com";
        private const string OrderNumber = "PO0001";
        private const string Language = "en";
        private const string PaymentResponseId = nameof(PaymentResponseId);

        private const string BillingAddressOrganization = nameof(BillingAddressOrganization);
        private const string BillingAddressLine1 = nameof(BillingAddressLine1);
        private const string BillingAddressLine2 = nameof(BillingAddressLine2);
        private const string BillingAddressCity = nameof(BillingAddressCity);
        private const string BillingAddressPostalCode = nameof(BillingAddressPostalCode);
        private const string BillingAddressCountryCode = nameof(BillingAddressCountryCode);
        private const string BillingAddressFirstName = nameof(BillingAddressFirstName);
        private const string BillingAddressLastName = nameof(BillingAddressLastName);
        private const string BillingAddressEmail = nameof(BillingAddressEmail);
        private const string BillingAddressDaytimePhoneNumber = nameof(BillingAddressDaytimePhoneNumber);

        private const string ShippingAddressOrganization = nameof(ShippingAddressOrganization);
        private const string ShippingAddressLine1 = nameof(ShippingAddressLine1);
        private const string ShippingAddressLine2 = nameof(ShippingAddressLine2);
        private const string ShippingAddressCity = nameof(ShippingAddressCity);
        private const string ShippingAddressPostalCode = nameof(ShippingAddressPostalCode);
        private const string ShippingAddressCountryCode = nameof(ShippingAddressCountryCode);
        private const string ShippingAddressFirstName = nameof(ShippingAddressFirstName);
        private const string ShippingAddressLastName = nameof(ShippingAddressLastName);
        private const string ShippingAddressEmail = nameof(ShippingAddressEmail);
        private const string ShippingAddressDaytimePhoneNumber = nameof(ShippingAddressDaytimePhoneNumber);

        private const string LineItemCode = nameof(LineItemCode);
        private const string LineItemDisplayName = nameof(LineItemDisplayName);
        private const decimal LineItemQuantity = 1;

        private readonly DateTime _customerContactBirthDate = DateTime.Now;

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public void When_Process_Order_Invoked_And_Redirect_Api_Key_Null_Must_Throw_Expected_Exception()
        {
            SetupPayment();
            SetupCart();

            var checkoutConfiguration = new CheckoutConfiguration
            {
                RedirectUrl = nameof(CheckoutConfiguration.RedirectUrl),
                ApiKey = null
            };

            A.CallTo(() => _checkoutConfigurationLoader.GetConfiguration(A<string>._)).Returns(checkoutConfiguration);

            var orderProcessingResult = _processOrderCheckout.Process(_cart, _payment);
            Assert.IsTrue(orderProcessingResult.IsSuccessful);
        }

        [ExpectedException(typeof(ApplicationException))]
        [TestMethod]
        public void When_Process_Order_Invoked_And_Redirect_Configuration_Null_Must_Throw_Expected_Exception()
        {
            SetupPayment();
            SetupCart();

            var checkoutConfiguration = new CheckoutConfiguration
            {
                RedirectUrl = null,
                ApiKey = nameof(CheckoutConfiguration.ApiKey)
            };

            A.CallTo(() => _checkoutConfigurationLoader.GetConfiguration(A<string>._)).Returns(checkoutConfiguration);

            var orderProcessingResult = _processOrderCheckout.Process(_cart, _payment);
            Assert.IsTrue(orderProcessingResult.IsSuccessful);
        }

        [ExpectedException(typeof(CultureNotFoundException))]
        [TestMethod]
        public void When_Process_Order_Invoked_And_Payment_Language_Is_Null_Must_Throw_Expected_Exception()
        {
            SetupConfiguration();
            SetupCart();

            var paymentProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => paymentProperties.Properties).Returns(new Hashtable
            {
                { Constants.OtherPaymentFields.LanguageId, null }
            });
            A.CallTo(() => _payment.Properties).Returns(paymentProperties.Properties);

            A.CallTo(() => _payment.Amount).Returns(10);

            _ = _processOrderCheckout.Process(_cart, _payment);
        }

        [TestMethod]
        public void When_Process_Order_Invoked_And_All_Is_Ok_Must_Return_Successful_Result()
        {
            SetupConfiguration();
            SetupPayment();
            SetupCart();

            var orderProcessingResult = _processOrderCheckout.Process(_cart, _payment);
            Assert.IsTrue(orderProcessingResult.IsSuccessful);
        }

        [TestInitialize]
        public void Setup()
        {
            _logger = A.Fake<ILogger>();
            _checkoutConfigurationLoader = A.Fake<ICheckoutConfigurationLoader>();
            _checkoutMetaDataFactory = A.Fake<ICheckoutMetaDataFactory>();
            _orderRepository = A.Fake<IOrderRepository>();
            _httpContextAccessor = A.Fake<ServiceAccessor<HttpContextBase>>();
            _httpClient = A.Fake<HttpClient>();
            _orderNoteHelper = A.Fake<IOrderNoteHelper>();
            _mollieOrderClient = A.Fake<IMollieOrderClient>();
            _marketService = A.Fake<IMarketService>();
            _paymentDescriptionGenerator = A.Fake<IPaymentDescriptionGenerator>();
            _productImageUrlFinder = A.Fake<IProductImageUrlFinder>();
            _productUrlGetter = A.Fake<IProductUrlGetter>();
            _currentCustomerContactGetter = A.Fake<ICurrentCustomerContactGetter>();

            var httpContext = new HttpContext(new HttpRequest(null, WebShopUrl, null), new HttpResponse(null));
            A.CallTo(() => _httpContextAccessor.Invoke()).Returns(new HttpContextWrapper(httpContext));

            A.CallTo(() => _paymentDescriptionGenerator.GetDescription(A<IOrderGroup>._, A<IPayment>._))
                .Returns(PaymentDescription);

            A.CallTo(() => _checkoutMetaDataFactory.Create(A<IOrderGroup>._, A<IPayment>._, A<CheckoutConfiguration>._))
                .Returns(new CheckoutMetaDataModel
                {
                    CartId = 1,
                    OrderNumber = nameof(CheckoutMetaDataModel.OrderNumber),
                    VersionStrings = nameof(CheckoutMetaDataModel.VersionStrings)
                });

            A.CallTo(() =>
                    _mollieOrderClient.CreateOrderAsync(A<OrderRequest>._, A<string>.Ignored, A<HttpClient>._))
                .Returns(Task.FromResult(new OrderResponse
                {
                    Id = PaymentResponseId,
                    Links = new OrderResponseLinks { Checkout = new UrlLink { Href = "https://www.mollie.com" } }
                }));


            A.CallTo(() =>
                    _mollieOrderClient.GetOrderAsync(A<string>.Ignored, A<string>.Ignored, A<HttpClient>._))
                .Returns(Task.FromResult(new OrderResponse
                {
                    Id = PaymentResponseId,
                    Links = new OrderResponseLinks { Checkout = new UrlLink { Href = "https://www.mollie.com" } }
                }));

            A.CallTo(() => _currentCustomerContactGetter.Get()).Returns(new CustomerContact
            {
                BirthDate = _customerContactBirthDate
            });

            _processOrderCheckout = new ProcessOrderCheckout(
                _logger,
                _checkoutConfigurationLoader,
                _paymentDescriptionGenerator,
                _checkoutMetaDataFactory,
                _orderRepository,
                _marketService,
                _httpContextAccessor,
                _productImageUrlFinder,
                _productUrlGetter,
                _httpClient,
                _orderNoteHelper,
                _mollieOrderClient,
                _currentCustomerContactGetter);

            _cart = A.Fake<ICart>();
            _payment = A.Fake<IPayment>();
        }

        private void SetupConfiguration()
        {
            var checkoutConfiguration = new CheckoutConfiguration
            {
                ApiKey = "e9150793-2705-47b8-8411-a2e02f9f68fd",
                RedirectUrl = RedirectUrl,
                OrderExpiresInDays = 30
            };

            A.CallTo(() => _checkoutConfigurationLoader.GetConfiguration(A<string>._)).Returns(checkoutConfiguration);
        }

        private void SetupCart()
        {
            A.CallTo(() => _cart.Currency).Returns(new Currency(CurrencyCode));

            var cartProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => cartProperties.Properties).Returns(new Hashtable
            {
                { "OrderNumber", OrderNumber }
            });
            A.CallTo(() => _cart.Properties).Returns(cartProperties.Properties);

            var orderForm = A.Fake<IOrderForm>();
            var shipment = A.Fake<IShipment>();
            var shipmentAddress = A.Fake<IOrderAddress>();
            var lineItem = A.Fake<ILineItem>();

            A.CallTo(() => lineItem.PlacedPrice).Returns(10);
            A.CallTo(() => lineItem.Code).Returns(LineItemCode);
            A.CallTo(() => lineItem.DisplayName).Returns(LineItemDisplayName);
            A.CallTo(() => lineItem.Quantity).Returns(LineItemQuantity);
            A.CallTo(() => shipment.LineItems).Returns(new List<ILineItem> {lineItem});

            A.CallTo(() => shipmentAddress.Organization).Returns(ShippingAddressOrganization);
            A.CallTo(() => shipmentAddress.Line1).Returns(ShippingAddressLine1);
            A.CallTo(() => shipmentAddress.Line2).Returns(ShippingAddressLine2);
            A.CallTo(() => shipmentAddress.City).Returns(ShippingAddressCity);
            A.CallTo(() => shipmentAddress.PostalCode).Returns(ShippingAddressPostalCode);
            A.CallTo(() => shipmentAddress.CountryCode).Returns(ShippingAddressCountryCode);
            A.CallTo(() => shipmentAddress.FirstName).Returns(ShippingAddressFirstName);
            A.CallTo(() => shipmentAddress.LastName).Returns(ShippingAddressLastName);
            A.CallTo(() => shipmentAddress.Email).Returns(ShippingAddressEmail);
            A.CallTo(() => shipmentAddress.DaytimePhoneNumber).Returns(ShippingAddressDaytimePhoneNumber);

            A.CallTo(() => shipment.ShippingAddress).Returns(shipmentAddress);
            A.CallTo(() => orderForm.Shipments).Returns(new List<IShipment> {shipment});
            A.CallTo(() => _cart.Forms).Returns(new List<IOrderForm>{ orderForm });
        }

        private void SetupPayment()
        {
            var paymentProperties = A.Fake<IExtendedProperties>();
            A.CallTo(() => paymentProperties.Properties).Returns(new Hashtable
            {
                { Constants.OtherPaymentFields.LanguageId, Language }
            });
            A.CallTo(() => _payment.Properties).Returns(paymentProperties.Properties);

            A.CallTo(() => _payment.Amount).Returns(Amount);

            var billingAddress = A.Fake<IOrderAddress>();
            A.CallTo(() => billingAddress.Organization).Returns(BillingAddressOrganization);
            A.CallTo(() => billingAddress.Line1).Returns(BillingAddressLine1);
            A.CallTo(() => billingAddress.Line2).Returns(BillingAddressLine2);
            A.CallTo(() => billingAddress.City).Returns(BillingAddressCity);
            A.CallTo(() => billingAddress.PostalCode).Returns(BillingAddressPostalCode);
            A.CallTo(() => billingAddress.CountryCode).Returns(BillingAddressCountryCode);
            A.CallTo(() => billingAddress.FirstName).Returns(BillingAddressFirstName);
            A.CallTo(() => billingAddress.LastName).Returns(BillingAddressLastName);
            A.CallTo(() => billingAddress.Email).Returns(BillingAddressEmail);
            A.CallTo(() => billingAddress.DaytimePhoneNumber).Returns(BillingAddressDaytimePhoneNumber);

            A.CallTo(() => _payment.BillingAddress).Returns(billingAddress);
        }
    }
}