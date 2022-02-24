var applePay = document.getElementById('payment-applepay');
if (applePay) {
    if (window.ApplePaySession && ApplePaySession.canMakePayments()) {
        alert('apple pay active');
        applePay.classList.remove("hidden");
    }
}