// Submits the Resource Guide feedback form in the background so visitors keep their search results.
// Requires _ReCaptchaScriptsPartial, which defines getReCaptchaToken.
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

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.querySelector("input[name=IsResourceFound]:checked")) {
            showStatus("Please let us know if you found what you were looking for.", true);
            form.querySelector("input[name=IsResourceFound]").focus();
            return;
        }

        submitButton.disabled = true;

        const formData = new FormData(form);
        try {
            formData.append("ReCaptchaToken", await getReCaptchaToken(form.dataset.recaptchaAction));
        } catch {
            // reCAPTCHA is blocked or not configured; the server decides whether to accept the feedback
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
        } catch {
            showStatus("Your feedback could not be submitted. Please check your connection and try again.", true);
        }

        submitButton.disabled = false;
    });
})();
