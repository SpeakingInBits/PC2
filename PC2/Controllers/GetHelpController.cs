using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PC2.Models;
using PC2.Services;

namespace PC2.Controllers
{
    /// <summary>
    /// The Get Help referral forms. Referrals are emailed to PC2 and are not saved on the website.
    /// </summary>
    public class GetHelpController : Controller
    {
        public const string SelfReCaptchaAction = "self_referral";
        public const string ProfessionalReCaptchaAction = "professional_referral";

        /// <summary>
        /// ViewData key that shows the "I'm not a robot" checkbox on a referral form after a low reCAPTCHA score
        /// </summary>
        public const string ShowReCaptchaCheckboxKey = "ShowReCaptchaCheckbox";

        private readonly IReCaptchaService _reCaptchaService;
        private readonly ReferralEmailService _referralEmailService;
        private readonly ILogger<GetHelpController> _logger;

        public GetHelpController(IReCaptchaService reCaptchaService, ReferralEmailService referralEmailService,
            ILogger<GetHelpController> logger)
        {
            _reCaptchaService = reCaptchaService;
            _referralEmailService = referralEmailService;
            _logger = logger;
        }

        /// <summary>
        /// Asks whether the visitor needs help themselves or is a professional making a referral
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Self()
        {
            return View(new SelfReferral());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Self(SelfReferral referral, CancellationToken cancellationToken)
        {
            return SubmitAsync(referral, SelfReCaptchaAction, cancellationToken);
        }

        [HttpGet]
        public IActionResult Professional()
        {
            return View(new ProfessionalReferral());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Professional(ProfessionalReferral referral, CancellationToken cancellationToken)
        {
            return SubmitAsync(referral, ProfessionalReCaptchaAction, cancellationToken);
        }

        public IActionResult Thanks()
        {
            return View();
        }

        /// <summary>
        /// Emails the referral to PC2, or shows the form again with the problems to fix
        /// </summary>
        private async Task<IActionResult> SubmitAsync(ReferralSubmission referral, string reCaptchaAction,
            CancellationToken cancellationToken)
        {
            RemoveInactiveFieldErrors(referral);
            AddObjectValidationErrors(referral);
            if (!ModelState.IsValid)
            {
                return View(referral);
            }

            // After a low score the form comes back with the "I'm not a robot" checkbox, and its token is sent instead
            ReCaptchaVerificationResult reCaptchaResult = string.IsNullOrEmpty(referral.ReCaptchaCheckboxToken)
                ? await _reCaptchaService.VerifyAsync(referral.ReCaptchaToken ?? "", reCaptchaAction, cancellationToken)
                : await _reCaptchaService.VerifyCheckboxAsync(referral.ReCaptchaCheckboxToken, cancellationToken);
            if (reCaptchaResult == ReCaptchaVerificationResult.ChallengeRequired)
            {
                ModelState.AddModelError(string.Empty,
                    "Please check the \"I'm not a robot\" box at the end of the form, then send it again.");
                ViewData[ShowReCaptchaCheckboxKey] = true;
                return View(referral);
            }
            if (reCaptchaResult == ReCaptchaVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty,
                    "We couldn't verify your form. Please try sending it again, or call us at 253.564.0707.");
                return View(referral);
            }
            if (reCaptchaResult == ReCaptchaVerificationResult.Unavailable)
            {
                // Don't turn away someone asking for help when Google can't be reached; the email notes it wasn't checked
                _logger.LogWarning("Sending a referral without reCAPTCHA verification.");
            }

            bool isSent = await _referralEmailService.SendAsync(referral,
                isSpamCheckSkipped: reCaptchaResult == ReCaptchaVerificationResult.Unavailable, cancellationToken);
            if (!isSent)
            {
                ModelState.AddModelError(string.Empty,
                    "Sorry, we couldn't send your form right now. Please try again later, or call us at 253.564.0707.");
                return View(referral);
            }

            return RedirectToAction(nameof(Thanks));
        }

        /// <summary>
        /// Ignores problems with answers in sections the visitor's answers hide, e.g. a mistyped email in the
        /// child's section after switching the referral to "Myself"
        /// </summary>
        private void RemoveInactiveFieldErrors(ReferralSubmission referral)
        {
            List<string> inactiveFields = referral.GetInactiveFields().ToList();
            foreach (string key in ModelState.Keys.ToList())
            {
                if (inactiveFields.Any(field => key == field || key.StartsWith(field + ".")))
                {
                    ModelState.Remove(key);
                }
            }
        }

        /// <summary>
        /// MVC only runs <see cref="IValidatableObject.Validate"/> when every field is valid, and errors in hidden
        /// sections could have stopped it running. Run it here so visitors see all of the problems at once.
        /// Problems MVC already found aren't added twice.
        /// </summary>
        private void AddObjectValidationErrors(ReferralSubmission referral)
        {
            foreach (ValidationResult result in referral.Validate(new ValidationContext(referral)))
            {
                foreach (string memberName in result.MemberNames)
                {
                    if (ModelState.GetFieldValidationState(memberName) != ModelValidationState.Invalid)
                    {
                        ModelState.AddModelError(memberName, result.ErrorMessage ?? "");
                    }
                }
            }
        }
    }
}
