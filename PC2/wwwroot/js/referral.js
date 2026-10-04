// Adds a reCAPTCHA token to the Get Help referral forms before they are sent.
// Requires _ReCaptchaScriptsPartial, which defines getReCaptchaToken.
(function () {
    // Move focus to the list of problems when the server sends the form back, so screen readers announce it
    const errors = document.getElementById("referral-errors");
    if (errors) {
        errors.focus();
    }

    const form = document.getElementById("referral-form");
    if (!form) {
        return;
    }

    const submitButton = form.querySelector("button[type=submit]");
    const submitText = submitButton.textContent;
    let isSubmitting = false;

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        // jQuery Validation shows the problems and focuses the first one
        if (isSubmitting || (window.jQuery && jQuery.fn.valid && !jQuery(form).valid())) {
            return;
        }

        isSubmitting = true;
        submitButton.disabled = true;
        submitButton.textContent = "Sending...";

        try {
            form.elements.ReCaptchaToken.value = await getReCaptchaToken(form.dataset.recaptchaAction);
        } catch {
            // reCAPTCHA is blocked or not configured; the server decides whether to accept the form
        }

        // Sends the form without raising the submit event again
        form.submit();
    });

    // Re-enable the button if the visitor comes back to this page with the browser's Back button
    window.addEventListener("pageshow", function (event) {
        if (event.persisted) {
            isSubmitting = false;
            submitButton.disabled = false;
            submitButton.textContent = submitText;
        }
    });
})();
