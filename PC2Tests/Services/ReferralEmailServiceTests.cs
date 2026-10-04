using IdentityLogin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PC2.Models;
using PC2.Services;
using System.Net;

namespace PC2Tests.Services;

[TestClass]
public class ReferralEmailServiceTests
{
    private const string PC2Email = "info@example.org";

    private Mock<IEmailSender> _emailSenderMock = null!;
    private string? _sentTo;
    private string? _sentSubject;
    private string? _sentPlainText;
    private string? _sentHtml;

    [TestInitialize]
    public void Setup()
    {
        _emailSenderMock = new Mock<IEmailSender>();
        SetupEmailResponse(HttpStatusCode.Accepted);
    }

    private void SetupEmailResponse(HttpStatusCode statusCode)
    {
        _emailSenderMock
            .Setup(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string, string>((to, subject, plainText, html) =>
            {
                _sentTo = to;
                _sentSubject = subject;
                _sentPlainText = plainText;
                _sentHtml = html;
            })
            .ReturnsAsync(new SendGrid.Response(statusCode, null, null));
    }

    private ReferralEmailService CreateService(string? pc2Email = PC2Email)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PC2Email"] = pc2Email })
            .Build();
        return new ReferralEmailService(_emailSenderMock.Object, config, Options.Create(new FeedbackDigestOptions()),
            TimeProvider.System, Mock.Of<ILogger<ReferralEmailService>>());
    }

    private static SelfReferral ChildReferral() => new()
    {
        ReferringFor = ReferralChoices.MyChild,
        Purpose = "Education support",
        BestTimesToContact = "Weekday mornings",
        HowHeard = "At school",
        Parent = new ReferralPerson
        {
            FirstName = "Jamie", LastName = "Rivera", ZipCode = "98418", MobileNumber = "(253) 555-0100", Email = "jamie@example.com"
        },
        AgeRange = "6-18",
        Child = new ReferralPerson
        {
            FirstName = "Sam", LastName = "Rivera", DateOfBirth = new DateTime(2012, 3, 4), Diagnosis = "Suspected",
            Race = "Two or more races", PrimaryLanguage = "Spanish", CountryOfOrigin = "Mexico"
        },
        // Typed before switching to "My child", so it should be left out of the email
        Self = new ReferralPerson { FirstName = "Hidden", Email = "hidden@example.com" },
        Situation = "Sam is <b>13</b>.\nWe need help with the IEP."
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
        Parent = new ReferralPerson { FirstName = "Alex", LastName = "Morgan", MobileNumber = "253-555-0123", Email = "alex@example.com" }
    };

    [TestMethod]
    public async Task SendAsync_ChildReferral_EmailsPC2()
    {
        bool isSent = await CreateService().SendAsync(ChildReferral(), isSpamCheckSkipped: false);

        Assert.IsTrue(isSent);
        Assert.AreEqual(PC2Email, _sentTo);
        Assert.AreEqual("New Get Help request: Jamie Rivera (for their child)", _sentSubject);
        StringAssert.Contains(_sentPlainText, "Self-referral");
        StringAssert.Contains(_sentPlainText, "PARENT OR CAREGIVER DETAILS");
        StringAssert.Contains(_sentPlainText, "CHILD DETAILS");
        StringAssert.Contains(_sentPlainText, "Date of birth: March 4, 2012");
        StringAssert.Contains(_sentPlainText, "Country of origin: Mexico");
        StringAssert.Contains(_sentHtml, "<a href=\"mailto:jamie@example.com\">jamie@example.com</a>");
        StringAssert.Contains(_sentHtml, "<a href=\"tel:2535550100\">(253) 555-0100</a>");
    }

    [TestMethod]
    public async Task SendAsync_LeavesOutSectionsHiddenByAnswers()
    {
        await CreateService().SendAsync(ChildReferral(), isSpamCheckSkipped: false);

        Assert.IsFalse(_sentPlainText!.Contains("Hidden"));
        Assert.IsFalse(_sentPlainText.Contains("YOUR DETAILS"));
    }

    [TestMethod]
    public async Task SendAsync_ProfessionalReferralWithConsent_IncludesParent()
    {
        bool isSent = await CreateService().SendAsync(CompleteProfessionalReferral(), isSpamCheckSkipped: false);

        Assert.IsTrue(isSent);
        Assert.AreEqual("New professional referral: Alex Morgan (from Tacoma Public Schools)", _sentSubject);
        StringAssert.Contains(_sentPlainText, "Professional referral");
        StringAssert.Contains(_sentPlainText, "REFERRED BY");
        StringAssert.Contains(_sentPlainText, "Name of your organization, agency, or affiliation: Tacoma Public Schools");
        StringAssert.Contains(_sentPlainText, "PARENT OR CAREGIVER DETAILS");
    }

    [TestMethod]
    public async Task SendAsync_ProfessionalReferralWithoutConsent_LeavesOutParent()
    {
        await CreateService().SendAsync(CompleteProfessionalReferral(hasConsent: "No"), isSpamCheckSkipped: false);

        Assert.AreEqual("New professional referral from Tacoma Public Schools", _sentSubject);
        Assert.IsFalse(_sentPlainText!.Contains("Alex"));
        StringAssert.Contains(_sentPlainText, "Did the family give you consent to share their information?: No");
    }

    [TestMethod]
    public async Task SendAsync_EncodesVisitorTextInHtml()
    {
        await CreateService().SendAsync(ChildReferral(), isSpamCheckSkipped: false);

        Assert.IsFalse(_sentHtml!.Contains("<b>13</b>"));
        StringAssert.Contains(_sentHtml, "Sam is &lt;b&gt;13&lt;/b&gt;.");
    }

    [TestMethod]
    public async Task SendAsync_SpamCheckSkipped_NotesItInEmail()
    {
        await CreateService().SendAsync(ChildReferral(), isSpamCheckSkipped: true);

        StringAssert.Contains(_sentPlainText, "could not check this submission for spam");
        StringAssert.Contains(_sentHtml, "could not check this submission for spam");
    }

    [TestMethod]
    public async Task SendAsync_SpamCheckPassed_DoesNotMentionSpam()
    {
        await CreateService().SendAsync(ChildReferral(), isSpamCheckSkipped: false);

        Assert.IsFalse(_sentPlainText!.Contains("spam"));
    }

    [TestMethod]
    public async Task SendAsync_EmailFails_ReturnsFalse()
    {
        SetupEmailResponse(HttpStatusCode.Unauthorized);

        Assert.IsFalse(await CreateService().SendAsync(ChildReferral(), isSpamCheckSkipped: false));
    }

    [TestMethod]
    public async Task SendAsync_EmailThrows_ReturnsFalse()
    {
        _emailSenderMock
            .Setup(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("SendGrid is down"));

        Assert.IsFalse(await CreateService().SendAsync(ChildReferral(), isSpamCheckSkipped: false));
    }

    [TestMethod]
    public async Task SendAsync_NoPC2Email_DoesNotSend()
    {
        Assert.IsFalse(await CreateService(pc2Email: null).SendAsync(ChildReferral(), isSpamCheckSkipped: false));
        _emailSenderMock.Verify(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public void BuildSubject_RemovesLineBreaks()
    {
        SelfReferral referral = ChildReferral();
        referral.Parent.FirstName = "Jamie\r\nBcc: someone@example.com";

        string subject = ReferralEmailService.BuildSubject(referral);

        Assert.IsFalse(subject.Contains('\n') || subject.Contains('\r'));
    }

    [TestMethod]
    public void BuildSections_LeavesOutUnansweredQuestions()
    {
        SelfReferral referral = ChildReferral();

        List<ReferralEmailField> parentFields = ReferralEmailService.BuildSections(referral)
            .Single(s => s.Heading == ReferralSubmission.ParentSection.Heading)
            .Fields.ToList();

        Assert.IsFalse(parentFields.Any(f => f.Label == "Date of birth"));
        Assert.IsTrue(parentFields.All(f => !string.IsNullOrWhiteSpace(f.Value)));
    }
}
