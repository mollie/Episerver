<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="PaymentMethodEditMolliePaymentMethods.ascx.cs" Inherits="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout.PaymentMethodEditMolliePaymentMethods" %>
<%@ Register TagPrefix="mc" Namespace="Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout" Assembly="Mollie.Checkout.CommerceManager" %>
<%@ Register TagPrefix="mc" Namespace="Mediachase.BusinessFoundation" Assembly="Mediachase.BusinessFoundation, Version=13.14.0.0, Culture=neutral, PublicKeyToken=41d2e7a615ba286c" %>

<h1>Mollie Payment Methods</h1>

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

<div id="DataForm">
	<table class="DataForm">
		<tr>
			<td class="FormLabelCell">
				<asp:Literal ID="litEntryMarkets" runat="server"></asp:Literal>
			</td>
			<td class="FormFieldCell">
				<mc:ListToListSelector runat="server" ID="ltlSelector" OneItemToTargetButtonId="btnMoveSelectedToTarget" OneItemToSourceButtonId="btnMoveSelectedToSource" SourceListId="lbSource" TargetListId="lbTarget" SelectAllSourceItemsButtonId="btnSelectSourceAll" SelectAllTargetItemsButtonId="btnSelectTargetAll">
				</mc:ListToListSelector>
				<table border="0">
					<tr>
						<td style="padding:10px;">
							<asp:Literal ID="Literal1" runat="server" Text="Unselected Payment Methods"></asp:Literal>&nbsp;&nbsp;<asp:Button ID="btnSelectSourceAll" runat="server" Text="Select All" OnClientClick="return false;" /><br />
							<div class="scrollable">
								<asp:ListBox runat="server" ID="lbSource" SelectionMode="Multiple" 
									DataTextField="Description" DataValueField="Id" Height="205" >
								</asp:ListBox>
							</div>
						</td>
						<td  style="vertical-align:middle; padding:10px; text-align:center;">
							<asp:Button runat="server" ID="btnMoveSelectedToTarget" Text=">" OnClientClick="return false;" /><br />
							<asp:Button runat="server" ID="btnMoveSelectedToSource" Text="<" OnClientClick="return false;"  />
						</td>
						<td style="padding:10px">
							<asp:Literal ID="Literal2"  runat="server" Text="Selected Payment Methods"></asp:Literal>&nbsp;&nbsp;<asp:Button ID="btnSelectTargetAll" runat="server" Text="Select All" OnClientClick="return false;" /><br />
							<div class="scrollable">
								<asp:ListBox runat="server" ID="lbTarget" DataTextField="Description" DataValueField="Id" SelectionMode="Multiple" Height="205px"></asp:ListBox>
							</div>
						</td>
					</tr>
				</table>
			</td>
			<td colspan="3" class="FormSpacerCell">
			</td>
		</tr>
		<tr>
			<td colspan="5" class="FormSpacerCell">
			</td>
		</tr>
	</table>
</div>
<script type="text/javascript">
    $(document).ready(function () {

        if (Mediachase && Mediachase.ListToListSelector) {
            Mediachase.ListToListSelector.prototype.AddOption = function (objTo, Option) {
                var oOption = document.createElement("OPTION");
                oOption.text = Option.text;
                oOption.value = Option.value;
                if (objTo != null)
                    objTo.options[objTo.options.length] = oOption;
            };
        }
    });

    function updateListBoxHeight() {
        var selectArr = $('.scrollable select');
        for (var i = 0; i < selectArr.length; i++) {
            selectArr[i].style.height = selectArr[i].scrollHeight + 'px';
        }
    }
</script>