using System.ComponentModel.DataAnnotations;
using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class ReferralTests
{
    /// <summary>
    /// A self-referral with the questions shared by every "Who are you making the referral for?" answer filled in
    /// </summary>
    private static SelfReferral CompleteSelfReferral(string referringFor)
    {
        SelfReferral referral = new()
        {
            ReferringFor = referringFor,
            Purpose = "Education support",
            BestTimesToContact = "Weekday mornings",
            HowHeard = "At school",
            Situation = "We need help with an IEP."
        };

        switch (referringFor)
        {
            case ReferralChoices.Myself:
                referral.Self = FullPerson();
                referral.HasChildWithDisability = "No";
                referral.CaresForAdult = "No";
                referral.PaidToCareForAdultChild = "No";
                break;
            case ReferralChoices.MyChild:
                referral.Parent = new ReferralPerson
                {
                    FirstName = "Jamie", LastName = "Rivera", ZipCode = "98418", MobileNumber = "253-555-0100", Email = "jamie@example.com"
                };
                referral.AgeRange = "6-18";
                referral.Child = PersonWithDisability();
                break;
            case ReferralChoices.FamilyMemberOrFriend:
                referral.Referrer = new ReferralPerson
                {
                    FirstName = "Jamie", LastName = "Rivera", MobileNumber = "253-555-0100", OkToText = "Yes",
                    Email = "jamie@example.com", ZipCode = "98418"
                };
                referral.FamilyMember = PersonWithDisability();
                referral.CaringWithoutPay = "Yes";
                break;
        }

        return referral;
    }

    private static ReferralPerson FullPerson() => new()
    {
        FirstName = "Jamie",
        LastName = "Rivera",
        DateOfBirth = new DateTime(1990, 5, 1),
        ZipCode = "98418",
        MobileNumber = "253-555-0100",
        OkToText = "Yes",
        Email = "jamie@example.com",
        Race = "Prefer not to say",
        PrimaryLanguage = "English",
        CountryOfOrigin = "United States"
    };

    private static ReferralPerson PersonWithDisability() => new()
    {
        FirstName = "Sam",
        LastName = "Rivera",
        DateOfBirth = new DateTime(2012, 3, 4),
        Diagnosis = "Suspected",
        Race = "Two or more races",
        PrimaryLanguage = "Spanish",
        CountryOfOrigin = "Mexico"
    };

    private static ProfessionalReferral CompleteProfessionalReferral(string hasConsent = "Yes") => new()
    {
        Purpose = "DDA services",
        BestTimesToContact = "Afternoons",
        HowHeard = "Community based organization",
        OrganizationName = "Tacoma Public Schools",
        FirstName = "Pat",
        LastName = "Lee",
        RoleOrRelationship = "Special education teacher",
        Phone = "253-555-0199",
        Email = "pat@example.org",
        FamilyPrimaryLanguage = "Somali",
        NeedsLanguageSupport = "Yes",
        Diagnosis = "Yes",
        AgeRange = "19-26",
        AdditionalInformation = "Finishing transition program.",
        HasConsent = hasConsent,
        Parent = hasConsent == "Yes"
            ? new ReferralPerson { FirstName = "Alex", LastName = "Morgan", ZipCode = "98404", MobileNumber = "253-555-0123", Email = "alex@example.com" }
            : new ReferralPerson()
    };

    /// <summary>
    /// Validates the referral the same way MVC does, including <see cref="IValidatableObject.Validate"/>
    /// </summary>
    private static List<ValidationResult> Validate(object referral)
    {
        List<ValidationResult> results = new();
        Validator.TryValidateObject(referral, new ValidationContext(referral), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string memberName) =>
        results.Any(r => r.MemberNames.Contains(memberName));

    [TestMethod]
    [DataRow(ReferralChoices.Myself)]
    [DataRow(ReferralChoices.MyChild)]
    [DataRow(ReferralChoices.FamilyMemberOrFriend)]
    public void SelfReferral_Complete_HasNoErrors(string referringFor)
    {
        List<ValidationResult> results = Validate(CompleteSelfReferral(referringFor));

        Assert.AreEqual(0, results.Count, string.Join(", ", results.Select(r => r.ErrorMessage)));
    }

    [TestMethod]
    public void SelfReferral_MissingQuestionsOnEveryForm_IsInvalid()
    {
        List<ValidationResult> results = Validate(new SelfReferral());

        Assert.IsTrue(HasErrorFor(results, nameof(SelfReferral.ReferringFor)));
        Assert.IsTrue(HasErrorFor(results, nameof(ReferralSubmission.Purpose)));
        Assert.IsTrue(HasErrorFor(results, nameof(ReferralSubmission.BestTimesToContact)));
        Assert.IsTrue(HasErrorFor(results, nameof(ReferralSubmission.HowHeard)));
        Assert.IsTrue(HasErrorFor(results, nameof(SelfReferral.Situation)));
    }

    [TestMethod]
    public void SelfReferral_Myself_RequiresEveryQuestionAboutThem()
    {
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.Myself);
        referral.Self = new ReferralPerson();
        referral.CaresForAdult = null;

        List<ValidationResult> results = Validate(referral);

        foreach (string field in SelfReferral.SelfSection.Fields)
        {
            Assert.IsTrue(HasErrorFor(results, $"Self.{field}"), field);
        }
        Assert.IsTrue(HasErrorFor(results, nameof(SelfReferral.CaresForAdult)));
    }

    [TestMethod]
    public void SelfReferral_MyChild_ParentDateOfBirthAndRaceAreOptional()
    {
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.MyChild);

        Assert.IsNull(referral.Parent.DateOfBirth);
        Assert.IsNull(referral.Parent.Race);
        Assert.AreEqual(0, Validate(referral).Count);
    }

    [TestMethod]
    public void SelfReferral_MyChild_RequiresChildDetailsAndAgeRange()
    {
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.MyChild);
        referral.Child.DateOfBirth = null;
        referral.Child.Diagnosis = null;
        referral.AgeRange = null;

        List<ValidationResult> results = Validate(referral);

        Assert.IsTrue(HasErrorFor(results, "Child.DateOfBirth"));
        Assert.IsTrue(HasErrorFor(results, "Child.Diagnosis"));
        Assert.IsTrue(HasErrorFor(results, nameof(SelfReferral.AgeRange)));
        Assert.IsTrue(results.Any(r => r.ErrorMessage == "Child's date of birth is required."));
    }

    [TestMethod]
    public void SelfReferral_OtherSectionsAreNotRequired()
    {
        // Choosing "Myself" doesn't require the child, parent, or family member sections
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.Myself);

        List<ValidationResult> results = Validate(referral);

        Assert.AreEqual(0, results.Count);
        CollectionAssert.IsSubsetOf(new[] { "Parent", "Child", "Referrer", "FamilyMember", "AgeRange" },
            referral.GetInactiveFields().ToList());
        CollectionAssert.DoesNotContain(referral.GetInactiveFields().ToList(), "Self");
    }

    [TestMethod]
    public void SelfReferral_FamilyMember_RequiresCaringWithoutPay()
    {
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.FamilyMemberOrFriend);
        referral.CaringWithoutPay = null;

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(SelfReferral.CaringWithoutPay)));
    }

    [TestMethod]
    [DataRow(nameof(SelfReferral.ReferringFor))]
    [DataRow(nameof(ReferralSubmission.Purpose))]
    [DataRow(nameof(ReferralSubmission.HowHeard))]
    [DataRow(nameof(SelfReferral.HasChildWithDisability))]
    public void SelfReferral_ChoiceNotOffered_IsInvalid(string propertyName)
    {
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.Myself);
        typeof(SelfReferral).GetProperty(propertyName)!.SetValue(referral, "Not a real option");

        Assert.IsTrue(HasErrorFor(Validate(referral), propertyName));
    }

    [TestMethod]
    public void SelfReferral_PersonChoiceNotOffered_IsInvalid()
    {
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.Myself);
        referral.Self.CountryOfOrigin = "Atlantis";

        Assert.IsTrue(HasErrorFor(Validate(referral), "Self.CountryOfOrigin"));
    }

    [TestMethod]
    public void SelfReferral_DateOfBirthInFuture_IsInvalid()
    {
        SelfReferral referral = CompleteSelfReferral(ReferralChoices.MyChild);
        referral.Child.DateOfBirth = DateTime.Today.AddDays(1);

        Assert.IsTrue(HasErrorFor(Validate(referral), "Child.DateOfBirth"));
    }

    [TestMethod]
    [DataRow("98418", true)]
    [DataRow("98418-1234", true)]
    [DataRow("9841", false)]
    [DataRow("Tacoma", false)]
    public void ReferralPerson_ZipCode_IsValidated(string zipCode, bool isValid)
    {
        ReferralPerson person = FullPerson();
        person.ZipCode = zipCode;

        Assert.AreEqual(isValid, !HasErrorFor(Validate(person), nameof(ReferralPerson.ZipCode)));
    }

    [TestMethod]
    public void ProfessionalReferral_Complete_HasNoErrors()
    {
        Assert.AreEqual(0, Validate(CompleteProfessionalReferral()).Count);
    }

    [TestMethod]
    public void ProfessionalReferral_WithConsent_RequiresParentContactDetails()
    {
        ProfessionalReferral referral = CompleteProfessionalReferral();
        referral.Parent = new ReferralPerson();

        List<ValidationResult> results = Validate(referral);

        Assert.IsTrue(HasErrorFor(results, "Parent.FirstName"));
        Assert.IsTrue(HasErrorFor(results, "Parent.MobileNumber"));
        Assert.IsTrue(HasErrorFor(results, "Parent.Email"));
        Assert.IsFalse(HasErrorFor(results, "Parent.DateOfBirth"));
    }

    [TestMethod]
    public void ProfessionalReferral_WithoutConsent_DoesNotAskForParentDetails()
    {
        ProfessionalReferral referral = CompleteProfessionalReferral(hasConsent: "No");

        Assert.AreEqual(0, Validate(referral).Count);
        CollectionAssert.Contains(referral.GetInactiveFields().ToList(), "Parent");
    }

    [TestMethod]
    public void ProfessionalReferral_MissingConsentAnswer_IsInvalid()
    {
        ProfessionalReferral referral = CompleteProfessionalReferral();
        referral.HasConsent = null;

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(ProfessionalReferral.HasConsent)));
    }

    [TestMethod]
    public void ReferralPersonSection_LabelsSayWhoTheQuestionIsAbout()
    {
        Assert.AreEqual("Parent/caregiver first name", ReferralSubmission.ParentSection.GetLabel(nameof(ReferralPerson.FirstName)));
        Assert.AreEqual("Child's date of birth", SelfReferral.ChildSection.GetLabel(nameof(ReferralPerson.DateOfBirth)));
        Assert.AreEqual("First name", SelfReferral.SelfSection.GetLabel(nameof(ReferralPerson.FirstName)));
        Assert.AreEqual("OK to send text messages is required.",
            SelfReferral.SelfSection.GetRequiredMessage(nameof(ReferralPerson.OkToText)));
    }
}
