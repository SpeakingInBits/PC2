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
                // Replace the form so the same feedback isn't submitted twice
                const thanks = document.createElement("p");
                thanks.className = "mb-0";
                thanks.setAttribute("role", "status");
                thanks.textContent = result.message || "Thank you for your feedback!";
                form.replaceWith(thanks);
                return;
            }

            showStatus(result.message || "Your feedback could not be submitted. Please try again.", true);
        } catch {
            showStatus("Your feedback could not be submitted. Please check your connection and try again.", true);
        }

        submitButton.disabled = false;
    });
})();
