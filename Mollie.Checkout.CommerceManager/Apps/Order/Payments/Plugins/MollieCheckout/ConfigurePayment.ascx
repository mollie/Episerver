<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout.ConfigurePayment" %>

<table id="GenericTable" runat="server">
    <tr>
        <td>
            <strong><asp:Literal ID="apiKeyLabel" Text="Api Key" runat="server"></asp:Literal></strong>
        </td>
        <td>
            <asp:TextBox ID="apiKeyTextbox" Text="Enter Api Key" runat="server"></asp:TextBox>
        </td>
    </tr>
    <tr>
        <td>
            <strong><asp:Literal ID="profileIDLabel" Text="Profile ID" runat="server"></asp:Literal></strong>
        </td>
        <td>
            <asp:TextBox ID="profileIDTextBox" Text="Enter Profile ID" runat="server"></asp:TextBox>
        </td>
    </tr>
</table>