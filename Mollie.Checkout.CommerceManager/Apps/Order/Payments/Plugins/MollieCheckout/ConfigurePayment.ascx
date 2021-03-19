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

    .FormFieldCell.top {
        vertical-align: top;
    }
</style>

<asp:HiddenField ID="PaymentMethodIdHiddenField" runat="server"/>

<table id="GenericTable" runat="server" class="mollie-payment-table">
    <tr>
        <td class="FormSectionCell" colspan="2">
            <h1><asp:Literal ID="settingsHeaderLabel" runat="server" Text="<%$ Resources: EPiServer, mollie.payment.settings.settingsheader %>"></asp:Literal></h1>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
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
            <asp:TextBox ID="apiKeyTextbox" CssClass="text" runat="server"></asp:TextBox>
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
            <asp:TextBox ID="profileIDTextBox" CssClass="text" runat="server"></asp:TextBox>
            <asp:RequiredFieldValidator ID="requiredProfileIDValidator" runat="server" ControlToValidate="profileIDTextBox" ErrorMessage="<%$ Resources: EPiServer, mollie.payment.settings.requiredprofileid %>" />
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="redirectURLLabel" Text="Redirect URL" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:TextBox ID="redirectURLTextBox" CssClass="text" runat="server"></asp:TextBox>
            <asp:RequiredFieldValidator ID="requiredRedirectURLValidator" runat="server" ControlToValidate="redirectURLTextBox" ErrorMessage="<%$ Resources: EPiServer, mollie.payment.settings.requiredredirecturl %>" />
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="orderExpiresInDaysLabel" Text="Order Expires In Days" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:TextBox ID="orderExpiresInDaysTextBox" CssClass="text" Text="30" runat="server"></asp:TextBox>
            <asp:RequiredFieldValidator ID="orderExpiresInDaysRequiredValidator" runat="server" ControlToValidate="orderExpiresInDaysTextBox" ErrorMessage="<%$ Resources: EPiServer, mollie.payment.settings.requiredorderexpires %>" />
            <asp:RangeValidator ID="orderExpiresInDaysRangeValidator" Type="Integer" MinimumValue="1" MaximumValue="100" runat="server" ControlToValidate="orderExpiresInDaysTextBox" ErrorMessage="<%$ Resources: EPiServer, mollie.payment.settings.rangeorderexpires %>" />
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
                <asp:ListItem Text="<%$ Resources: EPiServer, mollie.payment.settings.yes %>" Value="True" />
                <asp:ListItem Text="<%$ Resources: EPiServer, mollie.payment.settings.no %>" Value="False" Enabled="true" />
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
        <td class="FormFieldCell top">
            <asp:RadioButtonList ID="useCreditcardComponentsRadioButtonList" runat="server" RepeatDirection="Horizontal" Width="120px">
                <asp:ListItem Text="<%$ Resources: EPiServer, mollie.payment.settings.yes %>" Value="True" />
                <asp:ListItem Text="<%$ Resources: EPiServer, mollie.payment.settings.no %>" Value="False" Enabled="true" />
            </asp:RadioButtonList>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormSectionCell" colspan="2">
            <h1><asp:Literal ID="mollieInfoHeader" runat="server" Text="Mollie Info" /></h1>
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
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="linkToProfileLabel" Text="Link to Mollie Profile (to find the API keys)" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell top">
            <asp:HyperLink ID="linkToProfileHyperLink" CssClass="epi-visibleLink" Text="www.mollie.com/dashboard" NavigateUrl="https://www.mollie.com/dashboard/" Target="_blank" runat="server"></asp:HyperLink>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="linkToSupportPageLabel" Text="Link to our support page to find more info about Mollie" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell top">
            <asp:HyperLink ID="linkToSupportPageHyperLink" CssClass="epi-visibleLink" Text="help.mollie.com/hc/en-us" NavigateUrl="https://help.mollie.com/hc/en-us" Target="_blank" runat="server"></asp:HyperLink>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormSectionCell" colspan="2">
            <h2><asp:Literal ID="listedOnWebsiteHeader" runat="server" Text="Payment Methods listed on Website" /></h2>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td colspan="2">
            <p><b><i><asp:Literal ID="listedOnWebsiteDescription" runat="server" Text="Changes are visible after Settings have been saved!" /></i></b></p>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormLabelCell center">
            <strong><asp:Literal ID="marketCountryLabel" Text="Market - Country" runat="server"></asp:Literal></strong>
        </td>
        <td class="FormFieldCell">
            <asp:DropDownList AutoPostBack="True" ID="marketCountryDropDownList" OnSelectedIndexChanged="MarketCountryDropDownListSelectedIndexChanged" runat="server">
            </asp:DropDownList>
        </td>
    </tr>   
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormFieldCell" colspan="2">
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
                ItemsName="<%$ Resources: EPiServer, mollie.payment.settings.paymentmethodheader %>">
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
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormSectionCell" colspan="2">
            <h2><asp:Literal ID="notSupportedDescription" runat="server" Text="Not supported Currencies for Payment Methods by Market" /></h2>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
    <tr>
        <td class="FormFieldCell" colspan="2">
            <asp:Repeater ID="currencyValidationIssuesRepeater" runat="server" OnItemDataBound="currencyValidationIssuesRepeater_ItemDataBound">  
                <HeaderTemplate>
                    <table cellspacing="0" cellpadding="4" style="width:100%;border-collapse:collapse;">
                        <thead>
                            <tr>
                                <th class="ibn-vh2" scope="col"><asp:Literal ID="marketTableHeader" runat="server" Text="<%$ Resources: EPiServer, mollie.payment.settings.marketheader %>" /></th>
                                <th class="ibn-vh2" scope="col"><asp:Literal ID="currencyTableHeader" runat="server" Text="<%$ Resources: EPiServer, mollie.payment.settings.currencyheader %>" /></th>
                                <th class="ibn-vh2" scope="col"><asp:Literal ID="paymentMethodTableHeader" runat="server" Text="<%$ Resources: EPiServer, mollie.payment.settings.paymentmethodheader %>" /></th>
                            </tr>
                        </thead>
                </HeaderTemplate>
                <ItemTemplate>
                    <tr>
                        <td class="ibn-vb2"><%#Eval("Market") %></td>
                        <td class="ibn-vb2"><%#Eval("Currency") %></td>
                        <td class="ibn-vb2"><%#Eval("PaymentMethod") %></td>
                    </tr>
                </ItemTemplate>
                <FooterTemplate>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
        </td>
    </tr>
    <tr>
        <td colspan="2" class="FormSpacerCell"></td>
    </tr>
</table>