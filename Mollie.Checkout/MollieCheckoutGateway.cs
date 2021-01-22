using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Plugins.Payment;
using Mollie.Checkout.Services;
using System;

namespace Mollie.Checkout
{
    public class MollieCheckoutGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(MollieCheckoutGateway));

        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentDescriptionGenerator _paymentDescriptionGenerator;
        private readonly ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private readonly IOrderRepository _orderRepository;

        public MollieCheckoutGateway()
            : this(ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>(),
                ServiceLocator.Current.GetInstance<IPaymentDescriptionGenerator>(),
                ServiceLocator.Current.GetInstance<ICheckoutMetaDataFactory>(),
                ServiceLocator.Current.GetInstance<IOrderRepository>())
        { }

        public MollieCheckoutGateway(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IPaymentDescriptionGenerator paymentDescriptionGenerator,
            ICheckoutMetaDataFactory checkoutMetaDataFactory,
            IOrderRepository orderRepository)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _paymentDescriptionGenerator = paymentDescriptionGenerator;
            _checkoutMetaDataFactory = checkoutMetaDataFactory;
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Commerce 10 implementation
        /// </summary>
        public override bool ProcessPayment(Payment payment, ref string message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Commerce 11+ implementation
        /// </summary>
        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            if (null == orderGroup)
                throw new ArgumentNullException(nameof(orderGroup));

            if (!payment.Properties.ContainsKey(Constants.OtherPaymentFields.LanguageId) || 
                string.IsNullOrWhiteSpace(payment.Properties[Constants.OtherPaymentFields.LanguageId] as string))
            {
                throw new Exception("Payment propery LanguageId is not set");
            }
            

            
            var cart = orderGroup as ICart;
            // The order which is created by Commerce Manager
            if (cart == null && orderGroup is IPurchaseOrder)
            {
                if (payment.TransactionType == TransactionType.Capture.ToString())
                {
                    return ProcessPaymentCapture(orderGroup, payment);
                }

                // When "Refund" shipment in Commerce Manager, this method will be invoked with the TransactionType is Credit
                if (payment.TransactionType == TransactionType.Credit.ToString())
                {
                    return ProcessPaymentRefund(orderGroup, payment);
                }

                // Right now we do not support processing the order which is created by Commerce Manager
                return PaymentProcessingResult.CreateUnsuccessfulResult("The current payment method does not support order type.");
            }

            // CHECKOUT
            return ProcessPaymentCheckout(cart, payment);


        }

        private PaymentProcessingResult ProcessPaymentCheckout(ICart cart, IPayment payment)
        {
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;

            var checkoutConfiguration = _checkoutConfigurationLoader.GetConfiguration(languageId);

            var paymentClient = new Api.Client.PaymentClient(checkoutConfiguration.ApiKey);
            var paymentRequest = new Api.Models.Payment.Request.PaymentRequest
            {
                Amount = new Api.Models.Amount(cart.Currency.CurrencyCode, payment.Amount),
                Description = _paymentDescriptionGenerator.GetDescription(cart, payment),
                RedirectUrl = checkoutConfiguration.RedirectUrl + $"?orderNumber={cart.OrderNumber()}",
                WebhookUrl = $"http://foundation/api/molliewebhook/?languageId={languageId}",
                Metadata = _checkoutMetaDataFactory.Create(cart, payment, checkoutConfiguration)
            };

            var paymentResponse = paymentClient.CreatePaymentAsync(paymentRequest).Result;
            payment.Properties.Add(Constants.OtherPaymentFields.MolliePaymentId, paymentResponse.Id);
            _orderRepository.Save(cart);

            var message = $"---Mollie Create Payment is successful. Redirect end user to {paymentResponse.Links.Checkout}";
            _logger.Information(message);

            return PaymentProcessingResult.CreateSuccessfulResult(message, paymentResponse.Links.Checkout.ToString());
        }

        private PaymentProcessingResult ProcessPaymentRefund(IOrderGroup orderGroup, IPayment payment)
        {
            throw new NotImplementedException("Refunds not implemented yet");
        }

        private PaymentProcessingResult ProcessPaymentCapture(IOrderGroup orderGroup, IPayment payment)
        {
            throw new NotImplementedException("Capture not implemented yet");
        }




        private string GetOrderNumber(IOrderGroup orderGroup)
        {
            if (!string.IsNullOrWhiteSpace(orderGroup.Properties["OrderNumber"] as string))
                return orderGroup.Properties["OrderNumber"] as string;

            return orderGroup.OrderLink.OrderGroupId.ToString();
        }
    }
}
