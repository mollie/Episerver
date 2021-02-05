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

        public void Uninitialize(InitializationEngine context)
        {
            // Do nothing
        }
    }
}
