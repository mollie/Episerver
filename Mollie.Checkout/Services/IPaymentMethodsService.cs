﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Mediachase.Commerce;

namespace Mollie.Checkout.Services
{
    public interface IPaymentMethodsService
    {
        Task<List<Models.PaymentMethod>> LoadMethods(string languageId);

        Task<List<Models.PaymentMethod>> LoadMethods(string languageId, Money cartTotal);
    }
}
