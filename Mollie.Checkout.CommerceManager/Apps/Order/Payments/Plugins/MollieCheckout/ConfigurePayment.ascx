﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout.ConfigurePayment" %>

<style>
    input.text {
        width: 200px;
    }

    .FormLabelCell.center {
        vertical-align: middle;
        width: 120px;
    }
</style>

<h1>Mollie Episerver Settings</h1>

<table id="GenericTable" runat="server" class="mollie-payment-table">
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
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
<tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="apiKeyLabel" Text="Api Key" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:TextBox ID="apiKeyTextbox" CssClass="text" Text="Enter Api Key" runat="server"></asp:TextBox>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="profileIDLabel" Text="Profile ID" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:TextBox ID="profileIDTextBox" CssClass="text" Text="Enter Profile ID" runat="server"></asp:TextBox>
            <asp:RequiredFieldValidator ID="requiredProfileIDTextBox" runat="server" ControlToValidate="profileIDTextBox" ErrorMessage="Profile ID is required" />
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="versionLabel" Text="Version" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:Label ID="versionValueLabel" CssClass="text" Text="Api Key" runat="server"></asp:Label>
        </td>
    </tr>
</table>