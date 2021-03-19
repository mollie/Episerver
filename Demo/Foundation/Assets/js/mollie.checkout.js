function MollieCheckout(profileId, locale, testmode) {

    this.mollie = Mollie(profileId, { locale: locale, testmode: testmode });

    this.initComponents = function () {
        var cardNumber = this.mollie.createComponent('cardNumber');
        cardNumber.mount('#card-number');

        var cardHolder = this.mollie.createComponent('cardHolder');
        cardHolder.mount('#card-holder');

        var expiryDate = this.mollie.createComponent('expiryDate');
        expiryDate.mount('#expiry-date');

        var verificationCode = this.mollie.createComponent('verificationCode');
        verificationCode.mount('#verification-code');

        var tokenField = document.querySelector('#CreditCardComponentToken');

        var cardNumberValid = document.querySelector('#card-number-valid');
        var cardNumberError = document.querySelector('#card-number-error');
        cardNumber.addEventListener('change', async event => {
            if (event.error && event.touched) {
                cardNumberError.textContent = event.error;
                cardNumberValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                cardNumberError.textContent = '';
                cardNumberValid.checked = true;
                await this.tryGetToken();
            }
        });


        var cardHolderValid = document.querySelector('#card-holder-valid');
        var cardHolderError = document.querySelector('#card-holder-error');
        cardHolder.addEventListener('change', async event => {
            if (event.error && event.touched) {
                cardHolderError.textContent = event.error;
                cardHolderValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                cardHolderError.textContent = '';
                cardHolderValid.checked = true;
                await this.tryGetToken();
            }
        });

        var expiryDateValid = document.querySelector('#expiry-date-valid');
        var expiryDateError = document.querySelector('#expiry-date-error');
        expiryDate.addEventListener('change', async event => {
            if (event.error && event.touched) {
                expiryDateError.textContent = event.error;
                expiryDateValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                expiryDateError.textContent = '';
                expiryDateValid.checked = true;
                await this.tryGetToken();
            }
        });

        var verificationCodeValid = document.querySelector('#verification-code-valid');
        var verificationCodeError = document.querySelector('#verification-code-error');
        verificationCode.addEventListener('change', async event => {
            if (event.error && event.touched) {
                verificationCodeError.textContent = event.error;
                verificationCodeValid.checked = false;
                tokenField.value = '';
                return;
            } else if (event.touched && !event.error) {
                verificationCodeError.textContent = '';
                verificationCodeValid.checked = true;
                await this.tryGetToken();
            }
        });
    }


    this.tryGetToken = async function () {
        var a = document.querySelector('#card-holder-valid');
        var b = document.querySelector('#card-number-valid');
        var c = document.querySelector('#expiry-date-valid');
        var d = document.querySelector('#verification-code-valid');

        if (a.checked === false || b.checked === false || c.checked === false || d.checked === false) {
            return;
        }

        const { token, error } = await this.mollie.createToken();

        if (error) {
            alert(error.message);
            // Something wrong happened while creating the token. Handle this situation gracefully.
            return;
        }

        if (token) {
            var tokenField = document.querySelector('#CreditCardComponentToken');
            tokenField.value = token;
        }
    }
}
