using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    /// <summary>
    /// The answers offered on the Get Help referral forms. Submitted values are checked against these lists.
    /// </summary>
    public static class ReferralChoices
    {
        public const string OtherPurpose = "Other";

        public static readonly IReadOnlyList<string> Purposes =
        [
            "Finding resources and services",
            "DDA services and eligibility",
            "Education and IEP support",
            "Transition to adulthood",
            "Employment",
            "Housing and homeownership",
            "Person Centered Planning",
            "Government benefits (SSI, Medicaid)",
            "Advocacy and legal support",
            "Health care and mental health",
            "Family and caregiver support",
            OtherPurpose
        ];

        public static readonly IReadOnlyList<string> ReferringFor = ["Myself", "My child", "A family member or friend"];

        public static readonly IReadOnlyList<string> ContactMethods = ["Phone call", "Text message", "Email"];

        public static readonly IReadOnlyList<string> AgeRanges = ["Birth to 5", "6 to 17", "18 to 21", "22 and older"];

        public static readonly IReadOnlyList<string> Diagnosis = ["Yes", "Suspected", "No", "Not sure"];

        public static readonly IReadOnlyList<string> YesNoNotSure = ["Yes", "No", "Not sure"];

        public static readonly IReadOnlyList<string> HowHeard =
        [
            "Friend or family member",
            "School",
            "Doctor or health care provider",
            "DDA or a case manager",
            "Community organization",
            "Community event",
            "Website or internet search",
            "Social media",
            "Other"
        ];
    }

    /// <summary>
    /// The questions asked on both Get Help referral forms. The referral is emailed to PC2, not saved.
    /// </summary>
    public abstract class ReferralSubmission : IValidatableObject
    {
        public const int ShortTextMaxLength = 200;
        public const int AdditionalInformationMaxLength = 3000;

        [Display(Name = "What kind of help is needed?")]
        public List<string> Purposes { get; set; } = new();

        [Display(Name = "Other kind of help")]
        [StringLength(ShortTextMaxLength)]
        public string? OtherPurpose { get; set; }

        [Display(Name = "Age of the person with a disability")]
        public string? AgeRange { get; set; }

        [Display(Name = "Does the person have a diagnosis?")]
        public string? Diagnosis { get; set; }

        [Display(Name = "Primary language of the person or family")]
        [StringLength(100)]
        public string? PrimaryLanguage { get; set; }

        [Display(Name = "Is an interpreter needed?")]
        public string? NeedsInterpreter { get; set; }

        [Display(Name = "Best days and times to contact")]
        [StringLength(ShortTextMaxLength)]
        public string? BestTimesToContact { get; set; }

        [Display(Name = "How did you hear about PC2?")]
        public string? HowHeard { get; set; }

        [Display(Name = "Additional information")]
        [StringLength(AdditionalInformationMaxLength)]
        public string? AdditionalInformation { get; set; }

        /// <summary>
        /// The Google reCAPTCHA v3 token generated when the form was submitted
        /// </summary>
        public string? ReCaptchaToken { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Purposes.Any(p => ReferralChoices.Purposes.Contains(p)))
            {
                yield return new ValidationResult("Please choose at least one kind of help.", [nameof(Purposes)]);
            }
            else if (Purposes.Contains(ReferralChoices.OtherPurpose) && string.IsNullOrWhiteSpace(OtherPurpose))
            {
                yield return new ValidationResult("Please describe the other kind of help that is needed.", [nameof(OtherPurpose)]);
            }

            foreach (ValidationResult result in ValidateChoice(AgeRange, ReferralChoices.AgeRanges, nameof(AgeRange))
                         .Concat(ValidateChoice(Diagnosis, ReferralChoices.Diagnosis, nameof(Diagnosis)))
                         .Concat(ValidateChoice(NeedsInterpreter, ReferralChoices.YesNoNotSure, nameof(NeedsInterpreter)))
                         .Concat(ValidateChoice(HowHeard, ReferralChoices.HowHeard, nameof(HowHeard))))
            {
                yield return result;
            }
        }

        /// <summary>
        /// Rejects an answer that isn't one of the choices offered on the form. A blank answer is allowed.
        /// </summary>
        protected static IEnumerable<ValidationResult> ValidateChoice(string? value, IReadOnlyList<string> choices, string memberName)
        {
            if (!string.IsNullOrEmpty(value) && !choices.Contains(value))
            {
                yield return new ValidationResult("Please choose one of the options listed.", [memberName]);
            }
        }
    }

    /// <summary>
    /// A request for help from an individual or family member
    /// </summary>
    public class SelfReferral : ReferralSubmission
    {
        public const string ReferringForRequiredMessage = "Please tell us who needs help.";

        [Display(Name = "Who needs help?")]
        [Required(ErrorMessage = ReferringForRequiredMessage)]
        public string? ReferringFor { get; set; }

        [Display(Name = "Your first name")]
        [Required(ErrorMessage = "Please enter your first name.")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Your last name")]
        [Required(ErrorMessage = "Please enter your last name.")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Phone number")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string? Phone { get; set; }

        [Display(Name = "Email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        public string? Email { get; set; }

        [Display(Name = "Best way to contact you")]
        public string? PreferredContactMethod { get; set; }

        [Display(Name = "ZIP code")]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Please enter a 5 digit ZIP code.")]
        public string? ZipCode { get; set; }

        [Display(Name = "Name of the person who needs help")]
        [StringLength(ShortTextMaxLength)]
        public string? PersonName { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (ValidationResult result in base.Validate(validationContext))
            {
                yield return result;
            }

            if (string.IsNullOrWhiteSpace(Phone) && string.IsNullOrWhiteSpace(Email))
            {
                yield return new ValidationResult("Please enter a phone number or email address so we can contact you.", [nameof(Phone)]);
            }

            foreach (ValidationResult result in ValidateChoice(ReferringFor, ReferralChoices.ReferringFor, nameof(ReferringFor))
                         .Concat(ValidateChoice(PreferredContactMethod, ReferralChoices.ContactMethods, nameof(PreferredContactMethod))))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// A referral from a professional, such as a teacher, case manager, or health care provider
    /// </summary>
    public class ProfessionalReferral : ReferralSubmission
    {
        public const string ConsentRequiredMessage = "Please make sure the person or family agrees to this referral before sending it.";

        [Display(Name = "Organization or agency")]
        [Required(ErrorMessage = "Please enter your organization or agency.")]
        [StringLength(ShortTextMaxLength)]
        public string? OrganizationName { get; set; }

        [Display(Name = "Your first name")]
        [Required(ErrorMessage = "Please enter your first name.")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Your last name")]
        [Required(ErrorMessage = "Please enter your last name.")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Your role or relationship to the family")]
        [Required(ErrorMessage = "Please enter your role or relationship to the family.")]
        [StringLength(ShortTextMaxLength)]
        public string? RoleOrRelationship { get; set; }

        [Display(Name = "Your phone number")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string? Phone { get; set; }

        [Display(Name = "Your email address")]
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        public string? Email { get; set; }

        [Display(Name = "Who should PC2 contact?")]
        [Required(ErrorMessage = "Please enter the name of the person PC2 should contact.")]
        [StringLength(ShortTextMaxLength)]
        public string? ContactName { get; set; }

        [Display(Name = "Their phone number")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string? ContactPhone { get; set; }

        [Display(Name = "Their email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        public string? ContactEmail { get; set; }

        [Display(Name = "Best way to contact them")]
        public string? ContactPreferredMethod { get; set; }

        [Display(Name = "Their ZIP code")]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Please enter a 5 digit ZIP code.")]
        public string? ZipCode { get; set; }

        [Display(Name = "Name of the person with a disability")]
        [StringLength(ShortTextMaxLength)]
        public string? PersonName { get; set; }

        [Display(Name = "The person or family agreed to this referral and to sharing their information with PC2")]
        [Range(typeof(bool), "true", "true", ErrorMessage = ConsentRequiredMessage)]
        public bool HasConsent { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (ValidationResult result in base.Validate(validationContext))
            {
                yield return result;
            }

            if (string.IsNullOrWhiteSpace(ContactPhone) && string.IsNullOrWhiteSpace(ContactEmail))
            {
                yield return new ValidationResult("Please enter a phone number or email address for the person PC2 should contact.", [nameof(ContactPhone)]);
            }

            foreach (ValidationResult result in ValidateChoice(ContactPreferredMethod, ReferralChoices.ContactMethods, nameof(ContactPreferredMethod)))
            {
                yield return result;
            }
        }
    }
}
