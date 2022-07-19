var applepaydirect = document.querySelector('#applepaydirect');
if (applepaydirect) {
    if (window.ApplePaySession) {
        applepaydirect.classList.add("hidden");
        if (ApplePaySession.canMakePayments()) {
            applepaydirect.classList.remove("hidden");
        } else {
            // Check for the existence of the openPaymentSetup method.
            if (ApplePaySession.openPaymentSetup) {
                // Display the Set up Apple Pay Button here…
                ApplePaySession.openPaymentSetup(merchantIdentifier)
                    .then(function (success) {
                        if (success) {
                            // Open payment setup successful
                        } else {
                            // Open payment setup failed
                        }
                    })
                    .catch(function (e) {
                        // Open payment setup error handling
                    });
            }
        }
    }


    applepaydirect.addEventListener('click',
        e => {
            var request = {
                countryCode: "US",
                currencyCode: "EUR",
                supportedNetworks: ["amex", "maestro", "masterCard", "visa", "vPay"],
                merchantCapabilities: ["supports3DS"],
                total: {
                    label: "Optimizely Demo", amount: "0.01"
                }
                //,requiredShippingContactFields: ['postalAddress']
            }

            var session = new ApplePaySession(3, request);

            session.onvalidatemerchant = function (event) {
                $.ajax({
                    url: '/MollieApi/ValidateMerchant?validationUrl=' + event.validationURL,
                    method: "GET",
                    contentType: "application/json; charset=utf-8"
                }).then(function (merchantSession) {
                    session.completeMerchantValidation(merchantSession);
                }, function (error) {
                    alert("merchant validation unsuccessful: " + JSON.stringify(error));
                    session.abort();
                });
            };

            session.onpaymentauthorized = function (event) {
                //https://developer.apple.com/documentation/apple_pay_on_the_web/applepaypayment
                //TODO: Create payment and Order
                session.completePayment(ApplePaySession.STATUS_SUCCESS);
            }

            session.oncancel = function (event) {
                alert("payment cancel error ", event);
            }

            session.begin();
        });
}