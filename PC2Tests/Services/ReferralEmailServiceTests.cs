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

    private static SelfReferral SelfReferral() => new()
    {
        ReferringFor = "My child",
        FirstName = "Jamie",
        LastName = "Rivera",
        Phone = "(253) 555-0100",
        Email = "jamie@example.com",
        PersonName = "Sam Rivera",
        Purposes = [ReferralChoices.OtherPurpose, "Education and IEP support"],
        OtherPurpose = "Summer camps",
        AdditionalInformation = "Sam is <b>16</b>.\nWe need help with the IEP."
    };

    private static ProfessionalReferral ProfessionalReferral() => new()
    {
        OrganizationName = "Tacoma Public Schools",
        FirstName = "Pat",
        LastName = "Lee",
        RoleOrRelationship = "Special education teacher",
        Email = "pat@example.org",
        ContactName = "Alex Morgan",
        ContactPhone = "253-555-0199",
        Purposes = ["Transition to adulthood"],
        HasConsent = true
    };

    [TestMethod]
    public async Task SendAsync_SelfReferral_EmailsPC2()
    {
        bool isSent = await CreateService().SendAsync(SelfReferral(), isSpamCheckSkipped: false);

        Assert.IsTrue(isSent);
        Assert.AreEqual(PC2Email, _sentTo);
        Assert.AreEqual("New Get Help request: Jamie Rivera (for their child)", _sentSubject);
        StringAssert.Contains(_sentPlainText, "Self-referral");
        StringAssert.Contains(_sentPlainText, "Who needs help: My child");
        StringAssert.Contains(_sentPlainText, "Name: Sam Rivera");
        StringAssert.Contains(_sentHtml, "<a href=\"mailto:jamie@example.com\">jamie@example.com</a>");
        StringAssert.Contains(_sentHtml, "<a href=\"tel:2535550100\">(253) 555-0100</a>");
    }

    [TestMethod]
    public async Task SendAsync_ProfessionalReferral_IncludesReferrer()
    {
        bool isSent = await CreateService().SendAsync(ProfessionalReferral(), isSpamCheckSkipped: false);

        Assert.IsTrue(isSent);
        Assert.AreEqual("New professional referral: Alex Morgan (from Tacoma Public Schools)", _sentSubject);
        StringAssert.Contains(_sentPlainText, "Professional referral");
        StringAssert.Contains(_sentPlainText, "REFERRED BY");
        StringAssert.Contains(_sentPlainText, "Organization: Tacoma Public Schools");
        StringAssert.Contains(_sentPlainText, "Family agreed to the referral: Yes");
    }

    [TestMethod]
    public async Task SendAsync_EncodesVisitorTextInHtml()
    {
        await CreateService().SendAsync(SelfReferral(), isSpamCheckSkipped: false);

        Assert.IsFalse(_sentHtml!.Contains("<b>16</b>"));
        StringAssert.Contains(_sentHtml, "Sam is &lt;b&gt;16&lt;/b&gt;.");
    }

    [TestMethod]
    public async Task SendAsync_SpamCheckSkipped_NotesItInEmail()
    {
        await CreateService().SendAsync(SelfReferral(), isSpamCheckSkipped: true);

        StringAssert.Contains(_sentPlainText, "could not check this submission for spam");
        StringAssert.Contains(_sentHtml, "could not check this submission for spam");
    }

    [TestMethod]
    public async Task SendAsync_SpamCheckPassed_DoesNotMentionSpam()
    {
        await CreateService().SendAsync(SelfReferral(), isSpamCheckSkipped: false);

        Assert.IsFalse(_sentPlainText!.Contains("spam"));
    }

    [TestMethod]
    public async Task SendAsync_EmailFails_ReturnsFalse()
    {
        SetupEmailResponse(HttpStatusCode.Unauthorized);

        Assert.IsFalse(await CreateService().SendAsync(SelfReferral(), isSpamCheckSkipped: false));
    }

    [TestMethod]
    public async Task SendAsync_EmailThrows_ReturnsFalse()
    {
        _emailSenderMock
            .Setup(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("SendGrid is down"));

        Assert.IsFalse(await CreateService().SendAsync(SelfReferral(), isSpamCheckSkipped: false));
    }

    [TestMethod]
    public async Task SendAsync_NoPC2Email_DoesNotSend()
    {
        Assert.IsFalse(await CreateService(pc2Email: null).SendAsync(SelfReferral(), isSpamCheckSkipped: false));
        _emailSenderMock.Verify(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public void BuildSubject_RemovesLineBreaks()
    {
        SelfReferral referral = SelfReferral();
        referral.FirstName = "Jamie\r\nBcc: someone@example.com";

        string subject = ReferralEmailService.BuildSubject(referral);

        Assert.IsFalse(subject.Contains('\n') || subject.Contains('\r'));
    }

    [TestMethod]
    public void BuildSections_LeavesOutUnansweredQuestions()
    {
        SelfReferral referral = SelfReferral();
        referral.Email = null;
        referral.AgeRange = null;

        List<ReferralEmailField> fields = ReferralEmailService.BuildSections(referral).SelectMany(s => s.Fields).ToList();

        Assert.IsFalse(fields.Any(f => f.Label == "Email"));
        Assert.IsFalse(fields.Any(f => f.Label == "Age"));
        Assert.IsTrue(fields.All(f => !string.IsNullOrWhiteSpace(f.Value)));
    }

    [TestMethod]
    public void BuildSections_ListsPurposesInFormOrderWithOtherLast()
    {
        ReferralEmailField helpNeeded = ReferralEmailService.BuildSections(SelfReferral())
            .SelectMany(s => s.Fields)
            .Single(f => f.Label == "Help needed");

        Assert.AreEqual("Education and IEP support\nOther: Summer camps", helpNeeded.Value);
    }

    [TestMethod]
    public void BuildSections_IgnoresPurposesNotOffered()
    {
        SelfReferral referral = SelfReferral();
        referral.Purposes.Add("<script>alert(1)</script>");

        ReferralEmailField helpNeeded = ReferralEmailService.BuildSections(referral)
            .SelectMany(s => s.Fields)
            .Single(f => f.Label == "Help needed");

        Assert.IsFalse(helpNeeded.Value!.Contains("script"));
    }
}
