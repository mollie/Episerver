using Mediachase.Commerce.Orders.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mollie.Checkout
{
    public static class PaymentMethodDtoExtensions
    {
        public static PaymentMethodDto.PaymentMethodParameterRow GetParameter(this PaymentMethodDto paymentMethodDto, string parameterName)
        {
            var rows = paymentMethodDto.PaymentMethodParameter.Select($"Parameter='{parameterName}'");
            if (rows != null && rows.Length > 0)
            {
                return rows[0] as PaymentMethodDto.PaymentMethodParameterRow;
            }

            return null;
        }
    }
}
