//using System.Collections;
//using System.Net.Http;
//using EPiServer.Commerce.Order;
//using EPiServer.Commerce.Storage;
//using FakeItEasy;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Mollie.Api.Models;
//using Mollie.Checkout.MollieClients;
//using Mollie.Checkout.ProcessRefund;
//using Mollie.Checkout.Services;
//using Mollie.Checkout.Services.Interfaces;

//namespace Mollie.Checkout.Tests.ProcessRefund
//{
//    public class ProcessPaymentRefundTests
//    {
//        private IMollieRefundClient _mollieRefundClient;
//        private IOrderRepository _orderRepository;
//        private ICheckoutConfigurationLoader _checkoutConfigurationLoader;
//        private HttpClient _httpClient;
//        private IOrderNoteHelper _orderNoteHelper;
//        private ProcessPaymentRefund _processPaymentRefund;

//        private IPayment _payment;
//        private ICart _cart;

//        private const string Language = "en";
//        private const string CurrencyCode = "EUR";
//        private const string OrderNumber = "PO0001";

//        [TestMethod]
//        public void When_Process_Refund_Invoked_And_All_Is_Ok_Must_Return_Successful_Result()
//        {
//            SetupPayment();

//            var paymentProcessingResult = _processPaymentRefund.Process(_cart, _payment);
//            Assert.IsTrue(paymentProcessingResult.IsSuccessful);
//        }

//        [TestInitialize]
//        public void Setup()
//        {
//            _mollieRefundClient = A.Fake<IMollieRefundClient>();
//            _orderRepository = A.Fake<IOrderRepository>();
//            _checkoutConfigurationLoader = A.Fake<ICheckoutConfigurationLoader>();
//            _orderNoteHelper = A.Fake<IOrderNoteHelper>();
//            _httpClient = A.Fake<HttpClient>();

//            _processPaymentRefund = new ProcessPaymentRefund(
//                _mollieRefundClient,
//                _orderRepository,
//                _httpClient,
//                _checkoutConfigurationLoader,
//                _orderNoteHelper);

//            SetupPayment();
//        }

//        private void SetupCart()
//        {
//            A.CallTo(() => _cart.Currency).Returns(new Currency(CurrencyCode));

//            var cartProperties = A.Fake<IExtendedProperties>();
//            A.CallTo(() => cartProperties.Properties).Returns(new Hashtable
//            {
//                { "OrderNumber", OrderNumber }
//            });
//            A.CallTo(() => _cart.Properties).Returns(cartProperties.Properties);
//        }

//        private void SetupPayment()
//        {
//            var paymentProperties = A.Fake<IExtendedProperties>();
//            A.CallTo(() => paymentProperties.Properties).Returns(new Hashtable
//            {
//                { Constants.OtherPaymentFields.LanguageId, Language }
//            });
//            A.CallTo(() => _payment.Properties).Returns(paymentProperties.Properties);
//        }
//    }
//}
