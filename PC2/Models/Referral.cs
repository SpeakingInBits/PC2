using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PC2.Models
{
    /// <summary>
    /// The answers offered on the Get Help referral forms. Submitted values are checked against these lists.
    /// The questions and answers follow the referral form the client chose as an example (Open Doors for
    /// Multicultural Families) until the client asks for changes.
    /// </summary>
    public static class ReferralChoices
    {
        public const string Myself = "Myself";
        public const string MyChild = "My child";
        public const string FamilyMemberOrFriend = "Family member or friend";
        public const string Yes = "Yes";
        public const string No = "No";

        public static readonly IReadOnlyList<string> ReferringFor = [Myself, MyChild, FamilyMemberOrFriend];

        public static readonly IReadOnlyList<string> Purposes =
        [
            "Advocacy and legal supports",
            "Basic needs",
            "Child care",
            "DDA services",
            "Development support",
            "Education support",
            "Employment support",
            "Government benefits",
            "Health care",
            "Housing support",
            "Interpretation and translation services",
            "Mental health",
            "Other",
            "Parent support"
        ];

        public static readonly IReadOnlyList<string> HowHeard =
        [
            "At a community event",
            "At school",
            "Community based organization",
            "Community member",
            "Friend or relative",
            "Other",
            "Website or internet search"
        ];

        public static readonly IReadOnlyList<string> YesNo = [Yes, No];

        public static readonly IReadOnlyList<string> YesNoNotSure = [Yes, No, "Not sure"];

        public static readonly IReadOnlyList<string> Diagnosis = [Yes, No, "Suspected", "I don't know"];

        public static readonly IReadOnlyList<string> AgeRanges = ["0-5", "6-18", "19-26", "27+"];

        public static readonly IReadOnlyList<string> Races =
        [
            "American Indian or Alaska Native",
            "Asian",
            "Black or African American",
            "Latino, Latino American, or Hispanic",
            "Native Hawaiian or Pacific Islander",
            "Some other race",
            "Two or more races",
            "Unknown",
            "White or Caucasian",
            "Prefer not to say"
        ];

        public static readonly IReadOnlyList<string> Languages =
        [
            "English", "Spanish", "Somali", "Vietnamese", "Mandarin", "Cantonese",
            "Amharic", "Arabic", "ASL", "Bengali", "Dari", "French", "German", "Hindi", "Japanese", "Khmer", "Korean",
            "Kurdish", "Non-verbal", "Oromo", "Other", "Persian (Farsi)", "Polish", "Portuguese", "Punjabi", "Russian",
            "Soninke", "Swahili", "Tagalog", "Taiwanese", "Tigrinya", "Ukrainian"
        ];

        public static readonly IReadOnlyList<string> Countries =
        [
            "United States",
            "Afghanistan", "Albania", "Algeria", "Andorra", "Angola", "Antigua and Barbuda", "Argentina", "Armenia",
            "Australia", "Austria", "Azerbaijan", "Bahamas", "Bahrain", "Bangladesh", "Barbados", "Belarus", "Belgium",
            "Belize", "Benin", "Bhutan", "Bolivia", "Bosnia and Herzegovina", "Botswana", "Brazil", "Brunei", "Bulgaria",
            "Burkina Faso", "Burundi", "Cabo Verde", "Cambodia", "Cameroon", "Canada", "Central African Republic", "Chad",
            "Chile", "China", "Colombia", "Comoros", "Congo, Democratic Republic of the", "Congo, Republic of the",
            "Costa Rica", "Cote d'Ivoire", "Croatia", "Cuba", "Cyprus", "Czechia", "Denmark", "Djibouti", "Dominica",
            "Dominican Republic", "Ecuador", "Egypt", "El Salvador", "Equatorial Guinea", "Eritrea", "Estonia",
            "Eswatini", "Ethiopia", "Fiji", "Finland", "France", "Gabon", "Gambia", "Georgia", "Germany", "Ghana",
            "Greece", "Grenada", "Guatemala", "Guinea", "Guinea-Bissau", "Guyana", "Haiti", "Honduras", "Hungary",
            "Iceland", "India", "Indonesia", "Iran", "Iraq", "Ireland", "Israel", "Italy", "Jamaica", "Japan", "Jordan",
            "Kazakhstan", "Kenya", "Kiribati", "Kosovo", "Kuwait", "Kyrgyzstan", "Laos", "Latvia", "Lebanon", "Lesotho",
            "Liberia", "Libya", "Liechtenstein", "Lithuania", "Luxembourg", "Madagascar", "Malawi", "Malaysia",
            "Maldives", "Mali", "Malta", "Marshall Islands", "Mauritania", "Mauritius", "Mexico", "Micronesia",
            "Moldova", "Monaco", "Mongolia", "Montenegro", "Morocco", "Mozambique", "Myanmar (Burma)", "Namibia",
            "Nauru", "Nepal", "Netherlands", "New Zealand", "Nicaragua", "Niger", "Nigeria", "North Korea",
            "North Macedonia", "Norway", "Oman", "Pakistan", "Palau", "Palestine", "Panama", "Papua New Guinea",
            "Paraguay", "Peru", "Philippines", "Poland", "Portugal", "Qatar", "Romania", "Russia", "Rwanda",
            "Saint Kitts and Nevis", "Saint Lucia", "Saint Vincent and the Grenadines", "Samoa", "San Marino",
            "Sao Tome and Principe", "Saudi Arabia", "Senegal", "Serbia", "Seychelles", "Sierra Leone", "Singapore",
            "Slovakia", "Slovenia", "Solomon Islands", "Somalia", "South Africa", "South Korea", "South Sudan", "Spain",
            "Sri Lanka", "Sudan", "Suriname", "Sweden", "Switzerland", "Syria", "Taiwan", "Tajikistan", "Tanzania",
            "Thailand", "Timor-Leste", "Togo", "Tonga", "Trinidad and Tobago", "Tunisia", "Turkey", "Turkmenistan",
            "Tuvalu", "Uganda", "Ukraine", "United Arab Emirates", "United Kingdom", "Uruguay", "Uzbekistan", "Vanuatu",
            "Vatican City", "Venezuela", "Vietnam", "Yemen", "Zambia", "Zimbabwe"
        ];
    }

    /// <summary>
    /// The details asked about one person on a referral. Each <see cref="ReferralPersonSection"/> chooses which of
    /// these are shown and which are required.
    /// </summary>
    public class ReferralPerson
    {
        [Display(Name = "First name")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Last name")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Home ZIP code")]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Please enter a 5 digit ZIP code.")]
        public string? ZipCode { get; set; }

        [Display(Name = "Mobile number")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string? MobileNumber { get; set; }

        [Display(Name = "OK to send text messages?")]
        public string? OkToText { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        public string? Email { get; set; }

        [Display(Name = "Diagnosis")]
        public string? Diagnosis { get; set; }

        [Display(Name = "Race")]
        public string? Race { get; set; }

        [Display(Name = "Primary language")]
        public string? PrimaryLanguage { get; set; }

        [Display(Name = "Country of origin")]
        public string? CountryOfOrigin { get; set; }

        /// <summary>
        /// The answer choices for each question that has them
        /// </summary>
        public static IReadOnlyList<string>? GetChoices(string propertyName) => propertyName switch
        {
            nameof(OkToText) => ReferralChoices.YesNo,
            nameof(Diagnosis) => ReferralChoices.Diagnosis,
            nameof(Race) => ReferralChoices.Races,
            nameof(PrimaryLanguage) => ReferralChoices.Languages,
            nameof(CountryOfOrigin) => ReferralChoices.Countries,
            _ => null
        };

        /// <summary>
        /// The answer to a question, formatted for display
        /// </summary>
        public string? GetValue(string propertyName)
        {
            object? value = typeof(ReferralPerson).GetProperty(propertyName)!.GetValue(this);
            return value is DateTime date ? date.ToString("MMMM d, yyyy") : value as string;
        }

        public static string GetDisplayName(string propertyName) =>
            typeof(ReferralPerson).GetProperty(propertyName)!.GetCustomAttribute<DisplayAttribute>()!.Name!;
    }

    /// <summary>
    /// A group of questions about one person on a referral form, such as the parent or the child
    /// </summary>
    /// <param name="PropertyName">The <see cref="ReferralPerson"/> property on the referral, which is also the form field prefix</param>
    /// <param name="Heading">The heading shown above the questions</param>
    /// <param name="LabelPrefix">Put before each question so it's clear who it's about, e.g. "Child's"</param>
    /// <param name="Fields">The <see cref="ReferralPerson"/> questions to ask, in order</param>
    /// <param name="RequiredFields">The questions that must be answered</param>
    public record ReferralPersonSection(string PropertyName, string Heading, string? LabelPrefix,
        IReadOnlyList<string> Fields, IReadOnlyCollection<string> RequiredFields)
    {
        public bool IsRequired(string field) => RequiredFields.Contains(field);

        /// <summary>
        /// The question as shown on the form, e.g. "Child's date of birth"
        /// </summary>
        public string GetLabel(string field)
        {
            string displayName = ReferralPerson.GetDisplayName(field);
            return LabelPrefix == null ? displayName : $"{LabelPrefix} {char.ToLowerInvariant(displayName[0])}{displayName[1..]}";
        }

        public string GetRequiredMessage(string field) => $"{GetLabel(field).TrimEnd('?')} is required.";

        public IEnumerable<ValidationResult> Validate(ReferralPerson person)
        {
            foreach (string field in Fields)
            {
                string memberName = $"{PropertyName}.{field}";
                string? value = person.GetValue(field);
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (IsRequired(field))
                    {
                        yield return new ValidationResult(GetRequiredMessage(field), [memberName]);
                    }
                }
                else if (ReferralPerson.GetChoices(field) is { } choices && !choices.Contains(value))
                {
                    yield return new ValidationResult($"{GetLabel(field).TrimEnd('?')}: please choose one of the options listed.", [memberName]);
                }
            }

            if (Fields.Contains(nameof(ReferralPerson.DateOfBirth)) && person.DateOfBirth is { } dateOfBirth
                && (dateOfBirth.Date > DateTime.Today || dateOfBirth.Year < 1900))
            {
                yield return new ValidationResult($"{GetLabel(nameof(ReferralPerson.DateOfBirth))} must be a past date.",
                    [$"{PropertyName}.{nameof(ReferralPerson.DateOfBirth)}"]);
            }
        }
    }

    /// <summary>
    /// The questions asked on both Get Help referral forms. The referral is emailed to PC2, not saved.
    /// </summary>
    public abstract class ReferralSubmission : IValidatableObject
    {
        public const int ShortTextMaxLength = 255;
        public const int LongTextMaxLength = 5000;

        protected static readonly string[] AllPersonFields =
        [
            nameof(ReferralPerson.FirstName), nameof(ReferralPerson.LastName), nameof(ReferralPerson.DateOfBirth),
            nameof(ReferralPerson.ZipCode), nameof(ReferralPerson.MobileNumber), nameof(ReferralPerson.OkToText),
            nameof(ReferralPerson.Email), nameof(ReferralPerson.Race), nameof(ReferralPerson.PrimaryLanguage),
            nameof(ReferralPerson.CountryOfOrigin)
        ];

        protected static readonly string[] PersonWithDisabilityFields =
        [
            nameof(ReferralPerson.FirstName), nameof(ReferralPerson.LastName), nameof(ReferralPerson.DateOfBirth),
            nameof(ReferralPerson.Diagnosis), nameof(ReferralPerson.Race), nameof(ReferralPerson.PrimaryLanguage),
            nameof(ReferralPerson.CountryOfOrigin)
        ];

        /// <summary>
        /// The parent or caregiver, asked about on both forms
        /// </summary>
        public static readonly ReferralPersonSection ParentSection = new(nameof(Parent), "Parent or caregiver details",
            "Parent/caregiver", AllPersonFields,
            [
                nameof(ReferralPerson.FirstName), nameof(ReferralPerson.LastName), nameof(ReferralPerson.ZipCode),
                nameof(ReferralPerson.MobileNumber), nameof(ReferralPerson.Email)
            ]);

        [Display(Name = "Purpose of referral")]
        [Required(ErrorMessage = "Purpose of referral is required.")]
        public string? Purpose { get; set; }

        [Display(Name = "What are the best days and times to contact you?")]
        [Required(ErrorMessage = "Best days and times to contact you is required.")]
        [StringLength(ShortTextMaxLength)]
        public string? BestTimesToContact { get; set; }

        [Display(Name = "How did you hear about us?")]
        [Required(ErrorMessage = "How you heard about us is required.")]
        public string? HowHeard { get; set; }

        public ReferralPerson Parent { get; set; } = new();

        /// <summary>
        /// The Google reCAPTCHA v3 token generated when the form was submitted
        /// </summary>
        public string? ReCaptchaToken { get; set; }

        /// <summary>
        /// The token from the "I'm not a robot" checkbox, sent instead of a v3 token after a low score
        /// </summary>
        public string? ReCaptchaCheckboxToken { get; set; }

        /// <summary>
        /// The groups of questions about a person that apply, based on the answers that show or hide them
        /// </summary>
        public abstract IEnumerable<ReferralPersonSection> GetActivePersonSections();

        /// <summary>
        /// The form fields in sections that are hidden by the visitor's answers. Their values are ignored.
        /// </summary>
        public abstract IEnumerable<string> GetInactiveFields();

        /// <summary>
        /// The [Required] error message for a question, so radio buttons can show the same message in the browser
        /// </summary>
        public static string? GetRequiredMessage(Type referralType, string propertyName) =>
            referralType.GetProperty(propertyName)?.GetCustomAttribute<RequiredAttribute>()?.ErrorMessage;

        public ReferralPerson GetPerson(ReferralPersonSection section) =>
            (ReferralPerson)GetType().GetProperty(section.PropertyName)!.GetValue(this)!;

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            IEnumerable<ValidationResult> results = ValidateChoice(Purpose, ReferralChoices.Purposes, nameof(Purpose))
                .Concat(ValidateChoice(HowHeard, ReferralChoices.HowHeard, nameof(HowHeard)));
            foreach (ReferralPersonSection section in GetActivePersonSections())
            {
                results = results.Concat(section.Validate(GetPerson(section)));
            }
            return results;
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

        /// <summary>
        /// Requires an answer to a question that is only asked in some sections, and checks it is one of the choices
        /// </summary>
        protected static IEnumerable<ValidationResult> RequireChoice(string? value, IReadOnlyList<string> choices,
            string memberName, string requiredMessage)
        {
            return string.IsNullOrWhiteSpace(value)
                ? [new ValidationResult(requiredMessage, [memberName])]
                : ValidateChoice(value, choices, memberName);
        }
    }

    /// <summary>
    /// A request for help for yourself, your child, or a family member or friend
    /// </summary>
    public class SelfReferral : ReferralSubmission
    {
        public const string ReferringForRequiredMessage = "Who you are making the referral for is required.";
        public const string HasChildWithDisabilityRequiredMessage = "Whether you have a child with a disability is required.";
        public const string CaresForAdultRequiredMessage = "Whether you care for a person 18 or over is required.";
        public const string PaidToCareForAdultChildRequiredMessage = "Whether you get paid to care for your adult child is required.";
        public const string AgeRangeRequiredMessage = "Age range of the person with a disability is required.";
        public const string CaringWithoutPayRequiredMessage = "Whether you are caring for them without being paid is required.";

        public static readonly ReferralPersonSection SelfSection = new(nameof(Self), "Your details", null, AllPersonFields,
            AllPersonFields);

        public static readonly ReferralPersonSection ChildSection = new(nameof(Child), "Child details", "Child's",
            PersonWithDisabilityFields, PersonWithDisabilityFields);

        private static readonly string[] ReferrerFields =
        [
            nameof(ReferralPerson.FirstName), nameof(ReferralPerson.LastName), nameof(ReferralPerson.MobileNumber),
            nameof(ReferralPerson.OkToText), nameof(ReferralPerson.Email), nameof(ReferralPerson.ZipCode)
        ];

        public static readonly ReferralPersonSection ReferrerSection = new(nameof(Referrer), "Your details", "Your",
            ReferrerFields, ReferrerFields);

        public static readonly ReferralPersonSection FamilyMemberSection = new(nameof(FamilyMember),
            "Family member or friend details", "Family member/friend's", PersonWithDisabilityFields, PersonWithDisabilityFields);

        [Display(Name = "Who are you making the referral for?")]
        [Required(ErrorMessage = ReferringForRequiredMessage)]
        public string? ReferringFor { get; set; }

        /// <summary>
        /// The person asking for help for themselves
        /// </summary>
        public ReferralPerson Self { get; set; } = new();

        [Display(Name = "Do you have a child with a disability?")]
        public string? HasChildWithDisability { get; set; }

        [Display(Name = "Do you care for a person 18 or over?")]
        public string? CaresForAdult { get; set; }

        [Display(Name = "Do you get paid to care for your adult child with a disability?")]
        public string? PaidToCareForAdultChild { get; set; }

        [Display(Name = "Age range of the person with a disability")]
        public string? AgeRange { get; set; }

        public ReferralPerson Child { get; set; } = new();

        [Display(Name = "Is your child 18 or over, and are you a paid caregiver?")]
        public string? ChildIsAdultWithPaidCaregiver { get; set; }

        /// <summary>
        /// The family member or friend asking for help for someone else
        /// </summary>
        public ReferralPerson Referrer { get; set; } = new();

        public ReferralPerson FamilyMember { get; set; } = new();

        [Display(Name = "Are you caring for your family member or friend without being paid?")]
        public string? CaringWithoutPay { get; set; }

        [Display(Name = "Please tell us as much as you can about your situation")]
        [Required(ErrorMessage = "Information about your situation is required.")]
        [StringLength(LongTextMaxLength)]
        public string? Situation { get; set; }

        /// <summary>
        /// The fields shown for each answer to <see cref="ReferringFor"/>
        /// </summary>
        private static readonly Dictionary<string, string[]> FieldsShownFor = new()
        {
            [ReferralChoices.Myself] = [nameof(Self), nameof(HasChildWithDisability), nameof(CaresForAdult), nameof(PaidToCareForAdultChild)],
            [ReferralChoices.MyChild] = [nameof(Parent), nameof(AgeRange), nameof(Child), nameof(ChildIsAdultWithPaidCaregiver)],
            [ReferralChoices.FamilyMemberOrFriend] = [nameof(Referrer), nameof(FamilyMember), nameof(CaringWithoutPay)]
        };

        public override IEnumerable<ReferralPersonSection> GetActivePersonSections() => ReferringFor switch
        {
            ReferralChoices.Myself => [SelfSection],
            ReferralChoices.MyChild => [ParentSection, ChildSection],
            ReferralChoices.FamilyMemberOrFriend => [ReferrerSection, FamilyMemberSection],
            _ => []
        };

        public override IEnumerable<string> GetInactiveFields() =>
            FieldsShownFor.Where(f => f.Key != ReferringFor).SelectMany(f => f.Value);

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            IEnumerable<ValidationResult> results = base.Validate(validationContext)
                .Concat(ValidateChoice(ReferringFor, ReferralChoices.ReferringFor, nameof(ReferringFor)));

            switch (ReferringFor)
            {
                case ReferralChoices.Myself:
                    results = results
                        .Concat(RequireChoice(HasChildWithDisability, ReferralChoices.YesNo, nameof(HasChildWithDisability), HasChildWithDisabilityRequiredMessage))
                        .Concat(RequireChoice(CaresForAdult, ReferralChoices.YesNo, nameof(CaresForAdult), CaresForAdultRequiredMessage))
                        .Concat(RequireChoice(PaidToCareForAdultChild, ReferralChoices.YesNo, nameof(PaidToCareForAdultChild), PaidToCareForAdultChildRequiredMessage));
                    break;
                case ReferralChoices.MyChild:
                    results = results
                        .Concat(RequireChoice(AgeRange, ReferralChoices.AgeRanges, nameof(AgeRange), AgeRangeRequiredMessage))
                        .Concat(ValidateChoice(ChildIsAdultWithPaidCaregiver, ReferralChoices.YesNo, nameof(ChildIsAdultWithPaidCaregiver)));
                    break;
                case ReferralChoices.FamilyMemberOrFriend:
                    results = results
                        .Concat(RequireChoice(CaringWithoutPay, ReferralChoices.YesNo, nameof(CaringWithoutPay), CaringWithoutPayRequiredMessage));
                    break;
            }

            return results;
        }
    }

    /// <summary>
    /// A referral from a partner, school, or organization, such as a teacher, case manager, or health care provider
    /// </summary>
    public class ProfessionalReferral : ReferralSubmission
    {
        [Display(Name = "Name of your organization, agency, or affiliation")]
        [Required(ErrorMessage = "Name of your organization is required.")]
        [StringLength(ShortTextMaxLength)]
        public string? OrganizationName { get; set; }

        [Display(Name = "Your first name")]
        [Required(ErrorMessage = "Your first name is required.")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Your last name")]
        [Required(ErrorMessage = "Your last name is required.")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Display(Name = "What's your relationship to the referred family?")]
        [Required(ErrorMessage = "Your relationship to the referred family is required.")]
        [StringLength(ShortTextMaxLength)]
        public string? RoleOrRelationship { get; set; }

        [Display(Name = "Your phone number")]
        [Required(ErrorMessage = "Your phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string? Phone { get; set; }

        [Display(Name = "Your email address")]
        [Required(ErrorMessage = "Your email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        public string? Email { get; set; }

        [Display(Name = "Primary language of the family")]
        [Required(ErrorMessage = "Primary language of the family is required.")]
        public string? FamilyPrimaryLanguage { get; set; }

        [Display(Name = "Will the referred family need language support?")]
        [Required(ErrorMessage = "Whether the family will need language support is required.")]
        public string? NeedsLanguageSupport { get; set; }

        [Display(Name = "Diagnosis")]
        [Required(ErrorMessage = "Diagnosis is required.")]
        public string? Diagnosis { get; set; }

        [Display(Name = "Age range of the person with a disability")]
        [Required(ErrorMessage = "Age range of the person with a disability is required.")]
        public string? AgeRange { get; set; }

        [Display(Name = "Please give us any additional information and tell us how PC2 can help")]
        [Required(ErrorMessage = "Additional information is required.")]
        [StringLength(LongTextMaxLength)]
        public string? AdditionalInformation { get; set; }

        [Display(Name = "Did the family give you consent to share their information?")]
        [Required(ErrorMessage = "Whether the family gave consent to share their information is required.")]
        public string? HasConsent { get; set; }

        public bool IsConsentGiven => HasConsent == ReferralChoices.Yes;

        public override IEnumerable<ReferralPersonSection> GetActivePersonSections() =>
            IsConsentGiven ? [ParentSection] : [];

        public override IEnumerable<string> GetInactiveFields() =>
            IsConsentGiven ? [] : [nameof(Parent)];

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return base.Validate(validationContext)
                .Concat(ValidateChoice(FamilyPrimaryLanguage, ReferralChoices.Languages, nameof(FamilyPrimaryLanguage)))
                .Concat(ValidateChoice(NeedsLanguageSupport, ReferralChoices.YesNoNotSure, nameof(NeedsLanguageSupport)))
                .Concat(ValidateChoice(Diagnosis, ReferralChoices.Diagnosis, nameof(Diagnosis)))
                .Concat(ValidateChoice(AgeRange, ReferralChoices.AgeRanges, nameof(AgeRange)))
                .Concat(ValidateChoice(HasConsent, ReferralChoices.YesNo, nameof(HasConsent)));
        }
    }
}
