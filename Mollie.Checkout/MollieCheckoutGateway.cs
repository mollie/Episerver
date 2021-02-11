using EPiServer.Commerce.Order;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Plugins.Payment;
using Mediachase.Commerce.Security;
using Mollie.Checkout.Services;
using System;
using System.Web;
using Mollie.Checkout.ProcessCheckout.Helpers.Interfaces;

namespace Mollie.Checkout
{
    public class MollieCheckoutGateway : AbstractPaymentGateway, IPaymentPlugin
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(MollieCheckoutGateway));

        private readonly ICheckoutConfigurationLoader _checkoutConfigurationLoader;
        private readonly IPaymentDescriptionGenerator _paymentDescriptionGenerator;
        private readonly ICheckoutMetaDataFactory _checkoutMetaDataFactory;
        private readonly IOrderRepository _orderRepository;

        private readonly ServiceAccessor<HttpContextBase> _httpContextAccessor;
        private readonly IProcessCheckoutFactory _processCheckoutFactory;

        public MollieCheckoutGateway()
            : this(ServiceLocator.Current.GetInstance<ICheckoutConfigurationLoader>(),
                ServiceLocator.Current.GetInstance<IPaymentDescriptionGenerator>(),
                ServiceLocator.Current.GetInstance<ICheckoutMetaDataFactory>(),
                ServiceLocator.Current.GetInstance<IOrderRepository>(),
                ServiceLocator.Current.GetInstance<ServiceAccessor<HttpContextBase>>(),
                ServiceLocator.Current.GetInstance<IProcessCheckoutFactory>())
        { }

        public MollieCheckoutGateway(
            ICheckoutConfigurationLoader checkoutConfigurationLoader,
            IPaymentDescriptionGenerator paymentDescriptionGenerator,
            ICheckoutMetaDataFactory checkoutMetaDataFactory,
            IOrderRepository orderRepository,
            ServiceAccessor<HttpContextBase> httpContextAcessor,
            IProcessCheckoutFactory processCheckoutFactory)
        {
            _checkoutConfigurationLoader = checkoutConfigurationLoader;
            _paymentDescriptionGenerator = paymentDescriptionGenerator;
            _checkoutMetaDataFactory = checkoutMetaDataFactory;
            _orderRepository = orderRepository;
            _httpContextAccessor = httpContextAcessor;
            _processCheckoutFactory = processCheckoutFactory;
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
            {
                throw new ArgumentNullException(nameof(orderGroup));
            }

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
            var languageId = payment.Properties[Constants.OtherPaymentFields.LanguageId] as string;
            var processCheckout = _processCheckoutFactory.GetInstance(languageId);

            return processCheckout.Process(cart, payment);
        }

        private PaymentProcessingResult ProcessPaymentRefund(IOrderGroup orderGroup, IPayment payment)
        {
            throw new NotImplementedException("Refunds not implemented yet");
        }

        private PaymentProcessingResult ProcessPaymentCapture(IOrderGroup orderGroup, IPayment payment)
        {
            throw new NotImplementedException("Capture not implemented yet");
        }
    }
}
