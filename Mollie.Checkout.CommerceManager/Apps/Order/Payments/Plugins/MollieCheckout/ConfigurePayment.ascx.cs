using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Interfaces;
using System;
using System.Web.UI;

namespace Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout
{
    public partial class ConfigurePayment : OrderBaseUserControl, IGatewayControl
    {
        private PaymentMethodDto paymentMethodDto = null;

        public string ValidationGroup { get; set; } = string.Empty;

        public void LoadObject(object dto)
        {
            paymentMethodDto = dto as PaymentMethodDto;

            if (!Page.IsPostBack)
            {
                apiKeyTextbox.Text = GetParameterByName(Constants.ApiKeyField)?.Value ?? string.Empty;
            }
        }

        public void SaveChanges(object dto)
        {
            if (Visible)
            {
                paymentMethodDto = dto as PaymentMethodDto;
                if (paymentMethodDto != null && paymentMethodDto.PaymentMethodParameter != null)
                {
                    var paymentMethodId = Guid.Empty;
                    if (paymentMethodDto.PaymentMethod.Count > 0)
                    {
                        paymentMethodId = paymentMethodDto.PaymentMethod[0].PaymentMethodId;
                    }

                    SetParamValue(paymentMethodId, Constants.ApiKeyField, apiKeyTextbox.Text);
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        { }

        private PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(string name)
        {
            var rows = paymentMethodDto.PaymentMethodParameter.Select($"Parameter='{name}'");
            if (rows != null && rows.Length > 0)
            {
                return rows[0] as PaymentMethodDto.PaymentMethodParameterRow;
            }
            return null;
        }

        private void SetParamValue(Guid paymentMethodId, string paramName, string value)
        {
            var param = GetParameterByName(paramName);
            if (param != null)
            {
                param.Value = value;
                PaymentManager.SavePayment(paymentMethodDto);
            }
            else
            {
                var newRow = paymentMethodDto.PaymentMethodParameter.NewPaymentMethodParameterRow();
                newRow.PaymentMethodId = paymentMethodId;
                newRow.Parameter = paramName;
                newRow.Value = value;
                paymentMethodDto.PaymentMethodParameter.Rows.Add(newRow);
            }
        }
    }
}