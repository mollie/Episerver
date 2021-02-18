using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mollie.Checkout.Services
{
    public interface IPaymentMethodsService
    {
        Task<List<Models.PaymentMethod>> LoadMethods(string languageId);
    }
}
