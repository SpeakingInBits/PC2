using System.ComponentModel.DataAnnotations;
using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class ReferralTests
{
    private static SelfReferral ValidSelfReferral() => new()
    {
        ReferringFor = "My child",
        FirstName = "Jamie",
        LastName = "Rivera",
        Phone = "253-555-0100",
        Purposes = ["Education and IEP support"]
    };

    private static ProfessionalReferral ValidProfessionalReferral() => new()
    {
        OrganizationName = "Tacoma Public Schools",
        FirstName = "Pat",
        LastName = "Lee",
        RoleOrRelationship = "Special education teacher",
        Email = "pat@example.org",
        ContactName = "Alex Morgan",
        ContactEmail = "alex@example.com",
        Purposes = ["Transition to adulthood"],
        HasConsent = true
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
    public void SelfReferral_Valid_HasNoErrors()
    {
        Assert.AreEqual(0, Validate(ValidSelfReferral()).Count);
    }

    [TestMethod]
    public void SelfReferral_EmailInsteadOfPhone_IsValid()
    {
        SelfReferral referral = ValidSelfReferral();
        referral.Phone = null;
        referral.Email = "jamie@example.com";

        Assert.AreEqual(0, Validate(referral).Count);
    }

    [TestMethod]
    public void SelfReferral_NoPhoneOrEmail_IsInvalid()
    {
        SelfReferral referral = ValidSelfReferral();
        referral.Phone = " ";

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(SelfReferral.Phone)));
    }

    [TestMethod]
    public void SelfReferral_NoPurposes_IsInvalid()
    {
        SelfReferral referral = ValidSelfReferral();
        referral.Purposes = [];

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(ReferralSubmission.Purposes)));
    }

    [TestMethod]
    public void SelfReferral_OnlyUnknownPurposes_IsInvalid()
    {
        SelfReferral referral = ValidSelfReferral();
        referral.Purposes = ["Not a real option"];

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(ReferralSubmission.Purposes)));
    }

    [TestMethod]
    public void SelfReferral_OtherPurposeWithoutDescription_IsInvalid()
    {
        SelfReferral referral = ValidSelfReferral();
        referral.Purposes = [ReferralChoices.OtherPurpose];

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(ReferralSubmission.OtherPurpose)));

        referral.OtherPurpose = "Summer camps";
        Assert.AreEqual(0, Validate(referral).Count);
    }

    [TestMethod]
    [DataRow(nameof(SelfReferral.ReferringFor))]
    [DataRow(nameof(SelfReferral.PreferredContactMethod))]
    [DataRow(nameof(ReferralSubmission.AgeRange))]
    [DataRow(nameof(ReferralSubmission.Diagnosis))]
    [DataRow(nameof(ReferralSubmission.NeedsInterpreter))]
    [DataRow(nameof(ReferralSubmission.HowHeard))]
    public void SelfReferral_ChoiceNotOffered_IsInvalid(string propertyName)
    {
        SelfReferral referral = ValidSelfReferral();
        typeof(SelfReferral).GetProperty(propertyName)!.SetValue(referral, "Not a real option");

        Assert.IsTrue(HasErrorFor(Validate(referral), propertyName));
    }

    [TestMethod]
    public void SelfReferral_MissingRequiredFields_IsInvalid()
    {
        List<ValidationResult> results = Validate(new SelfReferral { Phone = "253-555-0100", Purposes = ["Employment"] });

        Assert.IsTrue(HasErrorFor(results, nameof(SelfReferral.ReferringFor)));
        Assert.IsTrue(HasErrorFor(results, nameof(SelfReferral.FirstName)));
        Assert.IsTrue(HasErrorFor(results, nameof(SelfReferral.LastName)));
    }

    [TestMethod]
    [DataRow("98418", true)]
    [DataRow("98418-1234", true)]
    [DataRow("9841", false)]
    [DataRow("Tacoma", false)]
    public void SelfReferral_ZipCode_IsValidated(string zipCode, bool isValid)
    {
        SelfReferral referral = ValidSelfReferral();
        referral.ZipCode = zipCode;

        Assert.AreEqual(isValid, !HasErrorFor(Validate(referral), nameof(SelfReferral.ZipCode)));
    }

    [TestMethod]
    public void ProfessionalReferral_Valid_HasNoErrors()
    {
        Assert.AreEqual(0, Validate(ValidProfessionalReferral()).Count);
    }

    [TestMethod]
    public void ProfessionalReferral_WithoutConsent_IsInvalid()
    {
        ProfessionalReferral referral = ValidProfessionalReferral();
        referral.HasConsent = false;

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(ProfessionalReferral.HasConsent)));
    }

    [TestMethod]
    public void ProfessionalReferral_NoWayToContactFamily_IsInvalid()
    {
        ProfessionalReferral referral = ValidProfessionalReferral();
        referral.ContactEmail = null;

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(ProfessionalReferral.ContactPhone)));
    }

    [TestMethod]
    public void ProfessionalReferral_WithoutTheirOwnEmail_IsInvalid()
    {
        ProfessionalReferral referral = ValidProfessionalReferral();
        referral.Email = null;

        Assert.IsTrue(HasErrorFor(Validate(referral), nameof(ProfessionalReferral.Email)));
    }
}
