using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Editor;
using EPiServer.Web.Mvc.Html;
using EPiServer.Web.Routing;
using Foundation.Commerce.Customer.Services;
using Foundation.Features.Checkout.Services;
using Foundation.Features.MyAccount.AddressBook;
using Foundation.Infrastructure.Services;
using System.Web.Mvc;

namespace Foundation.Features.MyAccount.OrderConfirmation
{
    public class OrderConfirmationController : OrderConfirmationControllerBase<OrderConfirmationPage>
    {
        private readonly ICampaignService _campaignService;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public OrderConfirmationController(
            ICampaignService campaignService,
            ConfirmationService confirmationService,
            IAddressBookService addressBookService,
            IOrderGroupCalculator orderGroupCalculator,
            UrlResolver urlResolver, ICustomerService customerService) :
            base(confirmationService, addressBookService, orderGroupCalculator, urlResolver, customerService)
        {
            _campaignService = campaignService;
        }

        public ActionResult Index(OrderConfirmationPage currentPage, string notificationMessage, string orderNumber)
        {
            IPurchaseOrder order = null;
            if (PageEditing.PageIsInEditMode)
            {
                order = _confirmationService.CreateFakePurchaseOrder();
            }
            else if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                if (int.TryParse(orderNumber, out int orderId))
                {
                    order = _confirmationService.GetOrder(orderId);
                }
                else
                {
                    order = _purchaseOrderRepository.Load(orderNumber);
                }
            }

            if (order != null && order.CustomerId == _customerService.CurrentContactId)
            {
                var viewModel = CreateViewModel(currentPage, order);
                viewModel.NotificationMessage = notificationMessage;

                _campaignService.UpdateLastOrderDate();
                _campaignService.UpdatePoint(decimal.ToInt16(viewModel.SubTotal.Amount));

                return View(viewModel);
            }

            return Redirect(Url.ContentUrl(ContentReference.StartPage));
        }
    }
}