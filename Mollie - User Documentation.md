
# Mollie Payment Plugin - User Documentation

## Disclaimer

Mollie Payment Plugin is a Project by [Arlanet - Part of 4NG](https://www.arlanet.com/) and [Mollie](https://www.mollie.com).

This is still Work in Progress, new Functionalities will be added, existing functionalities might be removed, and existing functionalities might change in the future.

## Introduction

Mollie Payment Plugin is for Episerver Commerce Manager. This Plugin provides an integration of the Mollie Payment Provider in Episerver Commerce Manager.

Mollie offers several Payment Methods like iDeal, Creditcard, Klarna etc.

This Plugin supports the Payments and Orders API. The Orders API provides a complete Integration with the Shipping and Returns Flow in Episerver Commerce Manager. The Payments API only supports a Payment Status Update.
  
## Create Mollie Payment Plugin in Episerver Commerce

In the Episerver Commerce Manager you have to create a Payment for the Mollie Payment Plugin.

![](https://imgur.com/d86eiW0.png)


## Configure Mollie Payment Plugin in Episerver Commerce

In the following Tabs you can provide technical Configuration for the Payment of the Mollie Payment Plugin.

### Overview

Here you can configure the following Settings:

* System Keyword
* Language
* Class Name
* Payment Class

The System Keyword should always be "MollieCheckout". The System Keyword is used to identify the Payment. 

Mollie Payment Plugin settings are stored per Language. Changing the Language wil result in loss of Configuration. 

The selected **Class Name** should always be "Mollie.Checkout.MollieCheckoutGateway". 

The selected **Payment Class** should always be "Mediachase.Commerce.Orders.OtherPayment".

![](https://imgur.com/UCK5TzV.png)

### Parameters

Here you can configure the following Settings:

* Environment
* API Key
* Profile ID
* Redirect URL
* Order Expires in Days
* Use Orders API
* Use Creditcard Components
* Choosing payment methods and sorting

The **Environment** should be "Test" for Development and "Live" for Production. The **API Key** and **Profile ID** will be provided in the Mollie Dashboard. These Settings should not be shared with others.

The **Redirect URL** is used to redirect to an Order Confirmation Page after the Payment has been processed.

The **Order Expires In Days** must be a value between 1 and 100. The Default is 30 days. After this Period an unpaid Order expires.

Mollie offers 2 Integrations. The Payment and Order API. The Payment API only offers a basic Integration. The Order API offers a more complete Integration with Orders, Shipments and Returns. Orders API is mandatory for Klarna - Pay later.

Mollie offers Creditcard Components Integration. Mollie Components is a set of Javascript APIs that allow you to add the fields needed for credit card holder data to your own checkout, in a way that is fully PCI-DSS SAQ-A compliant.

For each selected market, country and type of API (Order or payment) a list of payment methods can be chosen. Each chosen payment method wil be shown on the website in the same order as in this list.

If a currency is not supported by Mollie for the chosen payment method a warning wil be listed. This payment method is not visiable for the currency of the market on the website.

![](https://imgur.com/A8e0mOg.png)

### Markets

The selected Markets are used for the Payment Methods Selection.

![](https://imgur.com/fBX3mIX.png)

## Mollie Payment Plugin on the Checkout Page

The Sort Order in the chosen Payment Methods is the order shown on the Checkout Page. The Payments Methods are only shown if the Currency and Amount is supported by the Payment Method.

### Mollie Creditcard Components

When the **Use Creditcard Components** Option **Yes** is selected in the Mollie Payment Configuration extra Inputs are shown on the Checkout Page. These Options are **Card holder**, **Card number**, **Expiry date** and **Verification code**.

When the Option **No** is selected these Inputs are part of the Mollie Checkout Process.

![](https://imgur.com/UJ5RyM2.png)

### iDeal Issuers

iDeal Issuers are shown on the Website. After placing an Order the Customer is redirected to the selected Issuer where the Payment can be completed.

![](https://imgur.com/WbfPkTg.png)

## Use Payments API

The Payments API offers basic Payment handling. The Payment State is updated in Episerver Commerce and the Payment is visible in the Mollie Dashboard.

Order Information, Refunds and Shipping Updates are not supported by this API.

The selected value for **Use Orders API** is **No**.

## Use Orders API

The Orders API offers a complete Payment handling. Order and Shipping Information are visible in the Mollie dashboard. If the Returns Flow in Episerver Commerce results in a Refund, it is created in the Mollie Dashboard.

The selected value for **Use Orders API** is **Yes**.

### Shipment

The Shipment Flow in Episerver Commerce will update the Shipments in the Mollie Dashboard. Tracking and Shipping Method Information are visible in the Orders Section in the Mollie Dashboard.

### Returns / Refunds

The Returns Flow in Episerver Commerce will update the Refunds in the Mollie Dashboard. When a Refund is created, it will be processed by Mollie.