using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Interfaces;
using Mollie.Checkout.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Castle.Components.DictionaryAdapter;
using EPiServer.Data;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using Mollie.Api.Models.PaymentMethod;
using Mollie.Checkout.Dto;
using Mollie.Checkout.Helpers;
using Newtonsoft.Json;
using static Mediachase.Commerce.Orders.Dto.PaymentMethodDto;
using Mollie.Checkout.Models;
using Mollie.Checkout.Storage;

namespace Mollie.Checkout.CommerceManager.Apps.Order.Payments.Plugins.MollieCheckout
{
    public partial class ConfigurePayment : OrderBaseUserControl, IGatewayControl
    {
        private PaymentMethodDto _paymentMethodDto;
        private string _languageId;

        public string ValidationGroup { get; set; } = string.Empty;

        public void LoadObject(object dto)
        {
            _paymentMethodDto = dto as PaymentMethodDto;

            _languageId = _paymentMethodDto?.PaymentMethod.Rows.Count > 0 ? 
                _paymentMethodDto.PaymentMethod.Rows[0]["LanguageId"] as string : 
                string.Empty;

            if (Page.IsPostBack)
            {
                return;
            }

            SetMultilingualSettingsLabels();

            var apiKey = GetParameterByName(Constants.Fields.ApiKeyField)?.Value ?? string.Empty;
            var useOrdersApi = GetParameterByName(Constants.Fields.UseOrdersApiField)?.Value?.ToLower() == "true";

            apiKeyTextbox.Text = apiKey;
            useOrdersApiRadioButtonList.SelectedValue = useOrdersApi ? "True" : "False";

            environmentDropDownList.SelectedValue = GetParameterByName(Constants.Fields.EnvironmentField)?.Value ?? "test";

            profileIDTextBox.Text = GetParameterByName(Constants.Fields.ProfileIDField)?.Value ?? string.Empty;
            redirectURLTextBox.Text = GetParameterByName(Constants.Fields.RedirectURLField)?.Value ?? string.Empty;
            useCreditcardComponentsRadioButtonList.SelectedValue = GetParameterByName(Constants.Fields.UseCreditcardComponentsField)?.Value ?? "False";
            useApplePayDirectIntegrationRadioButtonList.SelectedValue = GetParameterByName(Constants.Fields.UseApplePayDirectIntegrationField)?.Value ?? "False";
            orderExpiresInDaysTextBox.Text = GetParameterByName(Constants.Fields.OrderExpiresInDaysField)?.Value ?? "28";
            versionValueLabel.Text = AssemblyVersionUtils.CreateVersionString();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            BindMarketCountryDropDownList();

            var marketId = marketCountryDropDownList.SelectedValue?.Split('|').FirstOrDefault();
            var countryCode = marketCountryDropDownList.SelectedValue?.Split('|').Skip(1).FirstOrDefault();

            var paymentMethodId = Guid.Empty;
            if (_paymentMethodDto.PaymentMethod.Count > 0)
            {
                paymentMethodId = _paymentMethodDto.PaymentMethod[0].PaymentMethodId;
            }

            PaymentMethodIdHiddenField.Value = paymentMethodId.ToString();

            BindMolliePaymentMethods(
                paymentMethodId,
                marketId,
                countryCode,
                useOrdersApi,
                apiKey);

            var currencyValidationIssues = GetCurrencyValidationIssues(
                apiKey,
                useOrdersApi).ToList();

            currencyValidationIssuesRepeater.DataSource = currencyValidationIssues;
            currencyValidationIssuesRepeater.DataBind();
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
            SetParamValue(paymentMethodId, Constants.Fields.UseApplePayDirectIntegrationField, useApplePayDirectIntegrationRadioButtonList.SelectedValue);
            SetParamValue(paymentMethodId, Constants.Fields.OrderExpiresInDaysField, orderExpiresInDaysTextBox.Text);

            var marketId = marketCountryDropDownList.SelectedValue?.Split('|').FirstOrDefault();
            var countryCode = marketCountryDropDownList.SelectedValue?.Split('|').Skip(1).FirstOrDefault();

            if (!bool.TryParse(useOrdersApiRadioButtonList.SelectedValue, out var useOrdersApi) ||
                string.IsNullOrWhiteSpace(marketId) 
                || string.IsNullOrWhiteSpace(countryCode))
            {
                return;
            }

            var paymentMethodsSettingsService = ServiceLocator.Current.GetInstance<IPaymentMethodsSettingsService>();

            var settings = paymentMethodsSettingsService.GetSettings(paymentMethodId);
            if (settings.Id == null)
            {
                settings.Id = Identity.NewIdentity(paymentMethodId);
            }

            settings.DisabledPaymentMethods = UpdateMolliePaymentMethods(
                marketId,
                countryCode,
                useOrdersApi,
                molliePaymentMethodList.LeftItems.Cast<ListItem>().ToList(),
                settings.DisabledPaymentMethods);

            settings.EnabledPaymentMethods = UpdateMolliePaymentMethods(
                marketId,
                countryCode,
                useOrdersApi,
                molliePaymentMethodList.RightItems.Cast<ListItem>().ToList(),
                settings.EnabledPaymentMethods);

            paymentMethodsSettingsService.SaveSettings(settings);
        }

        private static string UpdateMolliePaymentMethods(
            string marketId,
            string countryCode,
            bool useOrdersApi,
            IList<ListItem> items,
            string molliePaymentMethodsString)
        {
            var molliePaymentMethodsForCurrentMarketAndCountry = items
                .Select(i => new MolliePaymentMethod
                {
                    Id = i.Value,
                    Description = i.Text,
                    MarketId = marketId,
                    CountryCode = countryCode,
                    OrderApi = useOrdersApi,
                    Rank = items.IndexOf(i)
                }).ToList();

            var molliePaymentMethods = string.IsNullOrWhiteSpace(molliePaymentMethodsString)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(molliePaymentMethodsString);

            molliePaymentMethods.RemoveAll(pm =>
                pm.MarketId == marketId && pm.CountryCode == countryCode && pm.OrderApi == useOrdersApi);

            molliePaymentMethods.AddRange(molliePaymentMethodsForCurrentMarketAndCountry);

            return JsonConvert.SerializeObject(molliePaymentMethods);
        }

        private PaymentMethodParameterRow GetParameterByName(string name)
        {
            var rows = _paymentMethodDto.PaymentMethodParameter.Select($"Parameter='{name}'");

            if (rows != null && rows.Length > 0)
            {
                return rows[0] as PaymentMethodParameterRow;
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

        private List<ListItem> CreateMarketCountryList()
        {
            var marketRows = _paymentMethodDto.MarketPaymentMethods.Rows;
            var marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            var marketCountryList = new List<ListItem>();

            foreach (MarketPaymentMethodsRow marketRow in marketRows)
            {
                var market = marketService.GetMarket(new MarketId(marketRow.MarketId));

                foreach (var countryCode in market.Countries)
                {
                    marketCountryList.Add(new ListItem
                    {
                        Text = $"{market.MarketName} - {countryCode}",
                        Value = $"{market.MarketId}|{countryCode}"
                    });
                }
            }

            return marketCountryList;
        }

        private void BindMarketCountryDropDownList()
        {
            var marketCountryList = CreateMarketCountryList();

            marketCountryDropDownList.DataSource = marketCountryList;
            marketCountryDropDownList.DataTextField = "Text";
            marketCountryDropDownList.DataValueField = "Value";
            marketCountryDropDownList.SelectedValue = marketCountryList.FirstOrDefault()?.Value;
            marketCountryDropDownList.DataBind();
        }

        private void BindMolliePaymentMethods(
            Guid paymentMethodId,
            string marketId, 
            string countryCode,
            bool useOrdersApi,
            string apiKey)
        {

            if (string.IsNullOrWhiteSpace(marketId) 
                || string.IsNullOrWhiteSpace(countryCode)
                || string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            var paymentMethodsService = ServiceLocator.Current.GetInstance<IPaymentMethodsService>();
            var paymentMethodsSettingsService = ServiceLocator.Current.GetInstance<IPaymentMethodsSettingsService>();

            var settings = paymentMethodsSettingsService.GetSettings(paymentMethodId);

            var disabledMolliePaymentMethodsString = settings.DisabledPaymentMethods;
            var disabledMolliePaymentMethods = string.IsNullOrWhiteSpace(disabledMolliePaymentMethodsString)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(disabledMolliePaymentMethodsString);

            var enabledMolliePaymentMethodsString = settings.DisabledPaymentMethods;
            var enabledMolliePaymentMethods = string.IsNullOrWhiteSpace(enabledMolliePaymentMethodsString)
                ? new EditableList<MolliePaymentMethod>()
                : JsonConvert.DeserializeObject<List<MolliePaymentMethod>>(enabledMolliePaymentMethodsString);

            List<PaymentMethodResponse> allMolliePayments;

            try
            {
                allMolliePayments = AsyncHelper.RunSync(() => paymentMethodsService.LoadMethods(
                    _languageId,
                    Currency.Empty,
                    1000,
                    countryCode,
                    apiKey,
                    useOrdersApi,
                    false));
            }
            catch
            {
                allMolliePayments = new EditableList<PaymentMethodResponse>();
            }

            var disabled = disabledMolliePaymentMethods
                .Where(pm => pm.MarketId == marketId && pm.CountryCode == countryCode && pm.OrderApi == useOrdersApi)
                .OrderBy(pm => pm.Rank)
                .ToList();

            var disabledIds = disabled
                .Select(pm => pm.Id)
                .ToList();

            var disabledAndSorted = disabled
                .Where(pm => allMolliePayments.Any(x => x.Id == pm.Id))
                .OrderBy(pm => disabledIds.IndexOf(pm.Id));

            var enabled = enabledMolliePaymentMethods
                .Where(pm => pm.MarketId == marketId && pm.CountryCode == countryCode && pm.OrderApi == useOrdersApi)
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

        private IEnumerable<NotSupportedCurrenciesViewModel> GetCurrencyValidationIssues(
            string apiKey,
            bool useOrdersApi)
        {
            var marketId = marketCountryDropDownList.SelectedValue?.Split('|').FirstOrDefault();
            var countryCode = marketCountryDropDownList.SelectedValue?.Split('|').Skip(1).FirstOrDefault();

            var paymentMethodsService = ServiceLocator.Current.GetInstance<IPaymentMethodsService>();
            var marketService = ServiceLocator.Current.GetInstance<IMarketService>();

            if (string.IsNullOrWhiteSpace(marketId))
            {
                yield break;
            }

            var market = marketService.GetMarket(new MarketId(marketId));
            if (market == null)
            {
                yield break;
            }

            foreach (var validationIssue in paymentMethodsService.GetCurrencyValidationIssues(
                _languageId,
                countryCode,
                apiKey,
                useOrdersApi,
                market))
            {
                yield return new NotSupportedCurrenciesViewModel
                {
                    Market = market.MarketName,
                    Currency = validationIssue.Key,
                    PaymentMethod = validationIssue.Value
                };
            }
        }

        private void ReloadPaymentMethods()
        {
            var apiKey = apiKeyTextbox.Text;

            if (!bool.TryParse(useOrdersApiRadioButtonList.SelectedValue, out var useOrdersApi))
            {
                useOrdersApi = false;
            }

            var marketId = marketCountryDropDownList.SelectedValue?.Split('|').FirstOrDefault();
            var country = marketCountryDropDownList.SelectedValue?.Split('|').Skip(1).FirstOrDefault();

            if (!Guid.TryParse(PaymentMethodIdHiddenField.Value, out var paymentMethodId))
            {
                return;
            }

            BindMolliePaymentMethods(
                paymentMethodId,
                marketId,
                country,
                useOrdersApi,
                apiKey);

            var currencyValidationIssues = GetCurrencyValidationIssues(
                apiKey,
                useOrdersApi).ToList();

            currencyValidationIssuesRepeater.DataSource = currencyValidationIssues;
            currencyValidationIssuesRepeater.DataBind();
        }

        private void SetMultilingualSettingsLabels()
        {
            environmentLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/environment");
            apiKeyLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/apikey");
            profileIDLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/profileid");
            redirectURLLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/redirecturl");
            orderExpiresInDaysLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/orderexpiresindays");
            useOrdersApiLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/useordersapi");
            useCreditcardComponentsLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/usecreditcardcomponents");
            useApplePayDirectIntegrationLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/useapplepaydirectintegration");
            mollieInfoHeader.Text = LocalizationService.Current.GetString("/mollie/payment/settings/mollieinfoheader");
            versionLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/version");
            linkToProfileLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/linktoprofile");
            linkToSupportPageLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/linktosupportpage");
            listedOnWebsiteHeader.Text = LocalizationService.Current.GetString("/mollie/payment/settings/listedonwebsiteheader");
            listedOnWebsiteDescription.Text = LocalizationService.Current.GetString("/mollie/payment/settings/listedonwebsite");
            marketCountryLabel.Text = LocalizationService.Current.GetString("/mollie/payment/settings/marketcountry");
            notSupportedDescription.Text = LocalizationService.Current.GetString("/mollie/payment/settings/notsupported");
        }

        protected void OrdersApiRadioButtonListOnSelectedIndexChanged(object sender, EventArgs e)
        {
            ReloadPaymentMethods();
        }

        protected void MarketCountryDropDownListSelectedIndexChanged(object sender, EventArgs e)
        {
            ReloadPaymentMethods();
        }

        protected void currencyValidationIssuesRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e == null || e.Item == null)
            {

            }
        }
    }
}