using EPiServer.Commerce.Order;
using EPiServer.Security;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Security;
using System;

namespace Mollie.Checkout.Services
{
    public static class OrderNoteHelper
    {
        public static void AddNoteToOrder(IOrderGroup orderGroup, string title, string detail, Guid customerId)
        {
            var note = orderGroup.CreateOrderNote();

            note.Type = OrderNoteTypes.System.ToString();
            note.CustomerId = customerId != Guid.Empty ?
                customerId :
                PrincipalInfo.CurrentPrincipal.GetContactId();
            note.Title = !string.IsNullOrEmpty(title) ?
                title :
                detail.Substring(0, Math.Min(detail.Length, 24)) + "...";
            note.Detail = detail;
            note.Created = DateTime.UtcNow;

            orderGroup.Notes.Add(note);
        }

    }
}
