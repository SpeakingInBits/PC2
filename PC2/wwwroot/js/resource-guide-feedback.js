// Submits the Resource Guide feedback form in the background so visitors keep their search results.
// Requires _ReCaptchaScriptsPartial, which defines getReCaptchaToken and renderReCaptchaCheckbox.
(function () {
    const form = document.getElementById("feedback-form");
    if (!form) {
        return;
    }

    const status = document.getElementById("feedback-status");
    const submitButton = form.querySelector("button[type=submit]");

    function showStatus(message, isError) {
        status.textContent = message;
        status.className = "mt-3 alert " + (isError ? "alert-danger" : "alert-success");
    }

    // Shown after a low reCAPTCHA score so the visitor can prove they're human
    const checkbox = form.querySelector("[data-recaptcha-checkbox]");
    let checkboxWidgetId = null;

    async function showCheckbox() {
        checkbox.hidden = false;
        if (checkboxWidgetId === null) {
            checkboxWidgetId = await renderReCaptchaCheckbox(checkbox.querySelector("[data-recaptcha-checkbox-widget]"));
        } else {
            // A checkbox token only works once, so the visitor checks the box again
            grecaptcha.reset(checkboxWidgetId);
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.querySelector("input[name=IsResourceFound]:checked")) {
            showStatus("Please let us know if you found what you were looking for.", true);
            form.querySelector("input[name=IsResourceFound]").focus();
            return;
        }

        const formData = new FormData(form);
        if (checkboxWidgetId !== null) {
            const checkboxToken = grecaptcha.getResponse(checkboxWidgetId);
            if (!checkboxToken) {
                showStatus("Please check the \"I'm not a robot\" box.", true);
                checkbox.querySelector("iframe")?.focus();
                return;
            }
            formData.set("ReCaptchaCheckboxToken", checkboxToken);
        }

        submitButton.disabled = true;

        if (checkboxWidgetId === null) {
            try {
                formData.append("ReCaptchaToken", await getReCaptchaToken(form.dataset.recaptchaAction));
            } catch {
                // reCAPTCHA is blocked or not configured; the server decides whether to accept the feedback
            }
        }

        try {
            const response = await fetch(form.action, { method: "POST", body: formData });
            const result = await response.json().catch(() => ({}));

            if (response.ok) {
                // Hide the form so the same feedback isn't submitted twice. The thanks goes in the
                // status region (outside the form) and gets focus, since the focused button is now hidden.
                form.hidden = true;
                status.textContent = result.message || "Thank you for your feedback!";
                status.className = "mb-0";
                status.focus();
                return;
            }

            showStatus(result.message || "Your feedback could not be submitted. Please try again.", true);
            if (result.challengeRequired) {
                await showCheckbox().catch(function () {
                    // reCAPTCHA is blocked; the visitor still sees the message
                });
            }
        } catch {
            showStatus("Your feedback could not be submitted. Please check your connection and try again.", true);
        }

        submitButton.disabled = false;
    });
})();
