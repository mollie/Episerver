using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Mollie.Checkout.Services;

namespace Foundation.Features.Checkout.MollieCheckout
{
    public class MollieCheckoutController : Controller
    {
        private readonly IPaymentMethodsService _paymentMethodsService;

        public MollieCheckoutController(IPaymentMethodsService paymentMethodsService)
        {
            _paymentMethodsService = paymentMethodsService;
        }


        public async Task<ActionResult> PaymentMethods(string languageId)
        {
            var model = await _paymentMethodsService.LoadMethods(languageId);

            return PartialView("~/Features/Checkout/MollieCheckout/_MollieCheckoutPartial.cshtml", model);
        }
    }
}