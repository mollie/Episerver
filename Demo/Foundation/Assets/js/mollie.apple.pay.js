var applePay = document.getElementById('payment-applepay');
if (applePay) {
    if (window.ApplePaySession && ApplePaySession.canMakePayments()) {
        applePay.classList.remove("hidden");
    }
}