// Shows the sections of the Get Help referral forms that apply, and adds a reCAPTCHA token before they are sent.
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

    // Show only the sections that apply to the visitor's answers, e.g. the child's details after choosing "My child".
    // Each section names the radio buttons and value it depends on in data-show-when and data-show-value.
    // jQuery Validation skips fields in hidden sections, and the server ignores them.
    const conditionalSections = form.querySelectorAll("[data-show-when]");
    function showSectionsForAnswers() {
        conditionalSections.forEach(function (section) {
            const selected = form.querySelector(`input[name="${section.dataset.showWhen}"]:checked`);
            section.hidden = !selected || selected.value !== section.dataset.showValue;
        });
    }
    form.addEventListener("change", function (event) {
        if (event.target.type === "radio") {
            showSectionsForAnswers();
        }
    });
    showSectionsForAnswers();

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
