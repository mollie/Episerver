<%@ Control Language="C#" EnableViewState="true" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout.ConfigurePayment" %>
<%@ Register TagPrefix="mc" Namespace="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout" Assembly="Mollie.Checkout.CommerceManager" %>
<%@ Register TagPrefix="console" Namespace="Mediachase.Web.Console.Controls" Assembly="Mediachase.WebConsoleLib" %>


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
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="environmentLabel" Text="Environment" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
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
            <asp:RequiredFieldValidator ID="requiredProfileIDValidator" runat="server" ControlToValidate="profileIDTextBox" ErrorMessage="Profile ID is required" />
        </td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="redirectURLLabel" Text="Redirect URL" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:TextBox ID="redirectURLTextBox" CssClass="text" Text="Enter Redirect URL" runat="server"></asp:TextBox>
            <asp:RequiredFieldValidator ID="requiredRedirectURLValidator" runat="server" ControlToValidate="redirectURLTextBox" ErrorMessage="Redirect URL is required" />
        </td>
    </tr>

    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="orderExpiresInDaysLabel" Text="Order Expires In Days" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:TextBox ID="orderExpiresInDaysTextBox" CssClass="text" Text="30" runat="server"></asp:TextBox>
            <asp:RequiredFieldValidator ID="orderExpiresInDaysRequiredValidator" runat="server" ControlToValidate="orderExpiresInDaysTextBox" ErrorMessage="Order Expires In Days is required" />
            <asp:RangeValidator ID="orderExpiresInDaysRangeValidator" Type="Integer" MinimumValue="1" MaximumValue="100" runat="server" ControlToValidate="orderExpiresInDaysTextBox" ErrorMessage="Order Expires In Days must be between 1 and 100" />
        </td>
    </tr>

    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="useOrdersApiLabel" Text="Use Orders API" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:RadioButtonList ID="useOrdersApiRadioButtonList" OnSelectedIndexChanged="OrdersApiRadioButtonListOnSelectedIndexChanged" AutoPostBack="True" runat="server" RepeatDirection="Horizontal" Width="120px">
                <asp:ListItem Text="Yes" Value="True" />
                <asp:ListItem Text="No" Value="False" Enabled="true" />
            </asp:RadioButtonList>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="useCreditcardComponentsLabel" Text="Use Creditcard Components" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:RadioButtonList ID="useCreditcardComponentsRadioButtonList" runat="server" RepeatDirection="Horizontal" Width="120px">
                <asp:ListItem Text="Yes" Value="True" />
                <asp:ListItem Text="No" Value="False" Enabled="true" />
            </asp:RadioButtonList>
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
            <asp:Label ID="versionValueLabel" CssClass="text" Text="Version" runat="server"></asp:Label>
        </td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="linkToProfileLabel" Text="Link to Mollie Profile (to find the API keys)" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:HyperLink ID="linkToProfileHyperLink" CssClass="epi-visibleLink" Text="www.mollie.com/dashboard" NavigateUrl="https://www.mollie.com/dashboard/" Target="_blank" runat="server"></asp:HyperLink>
        </td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="linkToSupportPageLabel" Text="Link to our support page to find more info about Mollie" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:HyperLink ID="linkToSupportPageHyperLink" CssClass="epi-visibleLink" Text="help.mollie.com/hc/en-us" NavigateUrl="https://help.mollie.com/hc/en-us" Target="_blank" runat="server"></asp:HyperLink>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormSpacerCell" colspan="2">
            <h1>Payment Methods (save payment methods per locale)</h1>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="localeLabel" Text="Locale" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:DropDownList OnSelectedIndexChanged="LocaleDropDownListSelectedIndexChanged" AutoPostBack="True" ID="localeDropDownList" runat="server">
            </asp:DropDownList>
        </td>
    </tr>    
    <tr>
        <td class="FormSectionCell" colspan="2">
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
        </td>
    </tr>
</table>