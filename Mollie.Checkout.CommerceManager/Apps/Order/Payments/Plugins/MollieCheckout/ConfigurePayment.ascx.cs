using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Interfaces;
using Mollie.Checkout.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Web.UI.WebControls;
using Castle.Components.DictionaryAdapter;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mollie.Api.Client;
using Mollie.Api.Models.List;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.Dto;
using Mollie.Checkout.Helpers;
using Newtonsoft.Json;

namespace Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout
{
    public partial class ConfigurePayment : OrderBaseUserControl, IGatewayControl
    {
        private PaymentMethodDto _paymentMethodDto;

        public string ValidationGroup { get; set; } = string.Empty;

        public void LoadObject(object dto)
        {
            _paymentMethodDto = dto as PaymentMethodDto;

            if (Page.IsPostBack)
            {
                return;
            }

            var apiKey = GetParameterByName(Constants.Fields.ApiKeyField)?.Value ?? string.Empty;
            var useOrdersApi = GetParameterByName(Constants.Fields.UseOrdersApiField)?.Value?.ToLower() == "true";

            BindMolliePaymentMethods(_paymentMethodDto.Locale.TextInfo.CultureName, apiKey, useOrdersApi);

            apiKeyTextbox.Text = apiKey;
            useOrdersApiRadioButtonList.SelectedValue = useOrdersApi ? "True" : "False";

            environmentDropDownList.SelectedValue = GetParameterByName(Constants.Fields.EnvironmentField)?.Value ?? "test";

            profileIDTextBox.Text = GetParameterByName(Constants.Fields.ProfileIDField)?.Value ?? string.Empty;
            redirectURLTextBox.Text = GetParameterByName(Constants.Fields.RedirectURLField)?.Value ?? string.Empty;
            useCreditcardComponentsRadioButtonList.SelectedValue = GetParameterByName(Constants.Fields.UseCreditcardComponentsField)?.Value ?? "False";
            orderExpiresInDaysTextBox.Text = GetParameterByName(Constants.Fields.OrderExpiresInDaysField)?.Value ?? "30";
            versionValueLabel.Text = AssemblyVersionUtils.CreateVersionString();

            var test = GetCurrencyValidationIssues(_paymentMethodDto.Locale.TextInfo.CultureName).ToList();
        }


        private IEnumerable<string> GetCurrencyValidationIssues(
            string locale)
        {
            if (string.IsNullOrWhiteSpace(locale))
            {
                yield break;
            }

            var paymentMethodsService = ServiceLocator.Current.GetInstance<IPaymentMethodsService>();
            var marketService = ServiceLocator.Current.GetInstance<IMarketService>();

            foreach (DataRow row in _paymentMethodDto.MarketPaymentMethods.Rows)
            {
                var marketId = row["MarketId"] as string;
                if (string.IsNullOrWhiteSpace(marketId))
                {
                    continue;
                }

                var market = marketService.GetMarket(new MarketId(marketId));
                if (market == null)
                {
                    continue;
                }

                foreach (var validationIssue in paymentMethodsService.GetCurrencyValidationIssues(
                    locale,
                    market.Currencies))
                {
                    yield return $"{market.MarketName};{validationIssue.Key};{validationIssue.Value}";
                }
            }
        }

        public void SaveChanges(object dto)
        {
            if (!Visible)
            {
                return;
            }

            _paymentMethodDto = dto as PaymentMethodDto;

            if (_paymentMethodDto?.PaymentMethodParameter == null)
            {
                return;
            }

            var paymentMethodId = Guid.Empty;

            if (_paymentMethodDto.PaymentMethod.Count > 0)
            {
                paymentMethodId = _paymentMethodDto.PaymentMethod[0].PaymentMethodId;
            }

            SetParamValue(paymentMethodId, Constants.Fields.EnvironmentField, environmentDropDownList.SelectedValue);
            SetParamValue(paymentMethodId, Constants.Fields.ApiKeyField, apiKeyTextbox.Text);
            SetParamValue(paymentMethodId, Constants.Fields.ProfileIDField, profileIDTextBox.Text);
            SetParamValue(paymentMethodId, Constants.Fields.RedirectURLField, redirectURLTextBox.Text);
            SetParamValue(paymentMethodId, Constants.Fields.UseOrdersApiField, useOrdersApiRadioButtonList.SelectedValue);
            SetParamValue(paymentMethodId, Constants.Fields.UseCreditcardComponentsField, useCreditcardComponentsRadioButtonList.SelectedValue);
            SetParamValue(paymentMethodId, Constants.Fields.OrderExpiresInDaysField, orderExpiresInDaysTextBox.Text);

            UpdateMolliePaymentMethods(
                _paymentMethodDto.Locale.TextInfo.CultureName,
                paymentMethodId,
                molliePaymentMethodList.LeftItems.Cast<ListItem>().ToList(),
                Constants.Fields.DisabledMolliePaymentMethods);

            UpdateMolliePaymentMethods(
                _paymentMethodDto.Locale.TextInfo.CultureName,
                paymentMethodId,
                molliePaymentMethodList.RightItems.Cast<ListItem>().ToList(),
                Constants.Fields.EnabledMolliePaymentMethods);
        }

        private void UpdateMolliePaymentMethods(
            string locale, 
            Guid paymentMethodId, 
            IList<ListItem> items, 
            string parameterString)
        {
            var molliePaymentMethodsForCurrentLocale = items
                .Select(i => new MolliePaymentMethod
                {
                    Id = i.Value,
                    Description = i.Text,
                    Locale = locale,
                    Rank = items.IndexOf(i)
                }).ToList();

            var molliePaymentMethodsString = GetParameterByName(parameterString)?.Value;
            var molliePaymentMethods = string.IsNullOrWhiteSpace(molliePaymentMethodsString)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(molliePaymentMethodsString);

            molliePaymentMethods = molliePaymentMethods
                .Where(pm => pm.Locale != locale)
                .ToList();

            molliePaymentMethods.AddRange(molliePaymentMethodsForCurrentLocale);

            SetParamValue(paymentMethodId, parameterString, JsonConvert.SerializeObject(molliePaymentMethods));
        }

        private PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(string name)
        {
            var rows = _paymentMethodDto.PaymentMethodParameter.Select($"Parameter='{name}'");

            if (rows != null && rows.Length > 0)
            {
                return rows[0] as PaymentMethodDto.PaymentMethodParameterRow;
            }
            return null;
        }

        private void SetParamValue(Guid paymentMethodId, string paramName, string value)
        {
            var param = GetParameterByName(paramName);

            if (param != null)
            {
                param.Value = value;

                PaymentManager.SavePayment(_paymentMethodDto);
            }
            else
            {
                var newRow = _paymentMethodDto.PaymentMethodParameter.NewPaymentMethodParameterRow();

                newRow.PaymentMethodId = paymentMethodId;
                newRow.Parameter = paramName;
                newRow.Value = value;
                _paymentMethodDto.PaymentMethodParameter.Rows.Add(newRow);
            }
        }

        private void BindMolliePaymentMethods(string locale, string apiKey, bool useOrdersApi)
        {
            if (string.IsNullOrWhiteSpace(locale))
            {
                return;
            }

            var disabledMolliePaymentMethodsString = GetParameterByName(Constants.Fields.DisabledMolliePaymentMethods)?.Value;
            var disabledMolliePaymentMethods = string.IsNullOrWhiteSpace(disabledMolliePaymentMethodsString)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(disabledMolliePaymentMethodsString);

            var enabledMolliePaymentMethodsString = GetParameterByName(Constants.Fields.EnabledMolliePaymentMethods)?.Value;
            var enabledMolliePaymentMethods = string.IsNullOrWhiteSpace(enabledMolliePaymentMethodsString)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(enabledMolliePaymentMethodsString);

            var allMolliePayments = GetPaymentMethods(apiKey, locale, useOrdersApi)
                .ToList();

            var disabled = disabledMolliePaymentMethods
                .Where(pm => pm.Locale == locale)
                .OrderBy(pm => pm.Rank)
                .ToList();

            var disabledIds = disabled
                .Select(pm => pm.Id)
                .ToList();

            var disabledAndSorted = disabled
                .Where(pm => allMolliePayments.Any(x => x.Id == pm.Id))
                .OrderBy(pm => disabledIds.IndexOf(pm.Id));

            var enabled = enabledMolliePaymentMethods
                .Where(pm => pm.Locale == locale)
                .OrderBy(pm => pm.Rank)
                .ToList();

            var enabledIds = enabled
                .Select(pm => pm.Id)
                .ToList();

            var enabledAndSorted =
                allMolliePayments.Where(pm => disabled.All(x => x.Id != pm.Id))
                .OrderBy(pm => enabledIds.IndexOf(pm.Id))
                .ToList();

            molliePaymentMethodList.LeftDataSource = disabledAndSorted;
            molliePaymentMethodList.RightDataSource = enabledAndSorted;
            molliePaymentMethodList.DataBind();
        }

        private static IEnumerable<MolliePaymentMethod> GetPaymentMethods(
            string apiKey,
            string locale,
            bool useOrdersApi)
        {
            var httpClient = new HttpClient();
            var versionString = AssemblyVersionUtils.CreateVersionString();
            httpClient.DefaultRequestHeaders.Add("user-agent", versionString);

            var paymentMethodClient = new PaymentMethodClient(apiKey, httpClient);

            ListResponse<PaymentMethodResponse> paymentMethodResponses;

            try
            {
                paymentMethodResponses = AsyncHelper.RunSync(() => paymentMethodClient.GetPaymentMethodListAsync(
                    locale: locale,
                    resource: useOrdersApi ? Api.Models.Payment.Resource.Orders : Api.Models.Payment.Resource.Payments,
                    amount: null,
                    includeIssuers: false));
            }
            catch (MollieApiException)
            {
                paymentMethodResponses = new ListResponse<PaymentMethodResponse>
                {
                    Items = new EditableList<PaymentMethodResponse>()
                };
            }

            foreach (var paymentMethod in paymentMethodResponses.Items)
            {
                yield return new MolliePaymentMethod
                {
                    Id = paymentMethod.Id,
                    Description = paymentMethod.Description
                };
            }
        }

        protected void OrdersApiRadioButtonListOnSelectedIndexChanged(object sender, EventArgs e)
        {
            var apiKey = apiKeyTextbox.Text;
            if (!bool.TryParse(useOrdersApiRadioButtonList.SelectedValue, out var useOrdersApi))
            {
                useOrdersApi = false;
            }

            BindMolliePaymentMethods(_paymentMethodDto.Locale.TextInfo.CultureName, apiKey, useOrdersApi);
        }
    }
}