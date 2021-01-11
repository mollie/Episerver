<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout.ConfigurePayment" %>

<table id="GenericTable" runat="server">
    <tr>
        <td>
            <strong><asp:Literal ID="environmentLabel" Text="Mollie Environment" runat="server"></asp:Literal></strong>
        </td>
        <td>
            <asp:DropDownList ID="environmentDropDownList" runat="server">
                <asp:ListItem Text="Test" Value="test"></asp:ListItem>
                <asp:ListItem Text="Live" Value="live"></asp:ListItem>
            </asp:DropDownList>
        </td>
    </tr>
    <tr>
        <td>
            <strong><asp:Literal ID="apiKeyLabel" Text="Mollie Api Key" runat="server"></asp:Literal></strong>
        </td>
        <td>
            <asp:TextBox ID="apiKeyTextbox" Text="Enter Mollie Api Key" runat="server"></asp:TextBox>
        </td>
    </tr>
</table>