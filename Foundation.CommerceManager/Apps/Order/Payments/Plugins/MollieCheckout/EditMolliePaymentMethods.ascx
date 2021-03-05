<%@ Control EnableViewState="true" Language="C#" AutoEventWireup="true" CodeBehind="EditMolliePaymentMethods.ascx.cs" Inherits="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout.EditMolliePaymentMethods" %>
<%@ Register TagPrefix="mc" Namespace="Mediachase.BusinessFoundation" Assembly="Mediachase.BusinessFoundation, Version=13.14.0.0, Culture=neutral, PublicKeyToken=41d2e7a615ba286c" %>
<%@ Register TagPrefix="console" Namespace="Mediachase.Web.Console.Controls" Assembly="Mediachase.WebConsoleLib" %>

<h1>Mollie Payment Methods</h1>

<asp:HiddenField runat="server" ID="apiKeyHiddenField"/>
<asp:HiddenField runat="server" ID="userOrderApiHiddenField"/>

<table id="GenericTable" runat="server" >
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="localeLabel" Text="Locale" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:DropDownList OnSelectedIndexChanged="LocaleDropDownListSelectedIndexChanged" AutoPostBack="True" ID="localeDropDownList" runat="server">
            </asp:DropDownList>
        </td>
    </tr>
</table>

<console:DualList
    ID="molliePaymentMethodList"
    runat="server"
    ListRows="6"
    EnableMoveAll="True"
    CssClass="text"
    LeftDataTextField="Description"
    LeftDataValueField="Id"
    RightDataTextField="Description"
    RightDataValueField="Id"
    ItemsName="Payment Methods">
    <RightListStyle
        Font-Bold="True"
        Width="300px"
        Height="150px">
    </RightListStyle>
    <ButtonStyle
        Width="100px">
    </ButtonStyle>
    <LeftListStyle
        Width="300px"
        Height="150px">
    </LeftListStyle>
</console:DualList>
