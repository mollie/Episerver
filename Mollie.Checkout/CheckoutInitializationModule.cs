using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus.Configurator;

namespace Mollie.Checkout
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class CheckoutInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            InitializeOtherPaymentMetaClass();
            InitializeCartMetaClass();
            InitializePurchaseOrderMetaClass();
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Do nothing
        }

        private void InitializeOtherPaymentMetaClass()
        {
            var otherPaymentMetaClass = OrderContext.Current.OtherPaymentMetaClass;
            var metaDataContext = OrderContext.MetaDataContext;

            var languageIdField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.LanguageId);

            if (languageIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.LanguageId,
                    Constants.OtherPaymentFields.LanguageId, string.Empty, MetaDataType.ShortString, 10, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentIdField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentId);

            if (molliePaymentIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentId,
                    Constants.OtherPaymentFields.MolliePaymentId, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentStatusField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentStatus);

            if (molliePaymentStatusField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentStatus,
                    Constants.OtherPaymentFields.MolliePaymentStatus, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentMethodField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentMethod);

            if (molliePaymentMethodField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentMethod,
                    Constants.OtherPaymentFields.MolliePaymentMethod, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }

            var molliePaymentResultField = MetaField.Load(metaDataContext, Constants.OtherPaymentFields.MolliePaymentFullResult);

            if (molliePaymentResultField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.OtherPaymentFields.MolliePaymentFullResult,
                    Constants.OtherPaymentFields.MolliePaymentFullResult, string.Empty, MetaDataType.LongString, int.MaxValue, true, false, false, false);
                otherPaymentMetaClass.AddField(metaField);
            }
        }

        private void InitializeCartMetaClass()
        {
            var shoppingCartMetaClass = OrderContext.Current.ShoppingCartMetaClass;
            var metaDataContext = OrderContext.MetaDataContext;

            var mollieOrderIdField = MetaField.Load(metaDataContext, Constants.Cart.MollieOrderId);
            if (mollieOrderIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.Cart.MollieOrderId,
                    Constants.Cart.MollieOrderId, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);

                shoppingCartMetaClass.AddField(metaField);
            }

            var mollieOrderStatusField = MetaField.Load(metaDataContext, Constants.Cart.MollieOrderStatusField);
            if (mollieOrderStatusField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.Cart.MollieOrderStatusField,
                    Constants.Cart.MollieOrderStatusField, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);

                shoppingCartMetaClass.AddField(metaField);
            }
        }

        private void InitializePurchaseOrderMetaClass()
        {
            var purchaseOrderMetaClass = OrderContext.Current.PurchaseOrderMetaClass;
            var metaDataContext = OrderContext.MetaDataContext;

            var mollieOrderIdField = MetaField.Load(metaDataContext, Constants.MollieOrder.MollieOrderId);
            if (mollieOrderIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.MollieOrder.MollieOrderId,
                    Constants.MollieOrder.MollieOrderId, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);

                purchaseOrderMetaClass.AddField(metaField);
            }

            var languageIdField = MetaField.Load(metaDataContext, Constants.MollieOrder.LanguageId);
            if (languageIdField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.MollieOrder.LanguageId,
                    Constants.MollieOrder.LanguageId, string.Empty, MetaDataType.ShortString, 10, true, false, false, false);
                purchaseOrderMetaClass.AddField(metaField);
            }

            var mollieOrderStatusField = MetaField.Load(metaDataContext, Constants.Cart.MollieOrderStatusField);
            if (mollieOrderStatusField == null)
            {
                var metaField = MetaField.Create(metaDataContext, string.Empty, Constants.Cart.MollieOrderStatusField,
                    Constants.Cart.MollieOrderStatusField, string.Empty, MetaDataType.ShortString, 25, true, false, false, false);

                purchaseOrderMetaClass.AddField(metaField);
            }
        }
    }
}