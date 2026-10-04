using IdentityLogin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PC2.Controllers;
using PC2.Models;
using PC2.Services;
using System.Net;

namespace PC2Tests.Controllers;

[TestClass]
public class GetHelpControllerTests
{
    private Mock<IReCaptchaService> _reCaptchaMock = null!;
    private Mock<IEmailSender> _emailSenderMock = null!;
    private GetHelpController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _reCaptchaMock = new Mock<IReCaptchaService>();
        SetupReCaptcha(ReCaptchaVerificationResult.Passed);

        _emailSenderMock = new Mock<IEmailSender>();
        SetupEmailResponse(HttpStatusCode.Accepted);

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PC2Email"] = "info@example.org" })
            .Build();
        var emailService = new ReferralEmailService(_emailSenderMock.Object, config,
            Options.Create(new FeedbackDigestOptions()), TimeProvider.System, Mock.Of<ILogger<ReferralEmailService>>());

        _controller = new GetHelpController(_reCaptchaMock.Object, emailService, Mock.Of<ILogger<GetHelpController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [TestCleanup]
    public void Cleanup()
    {
        _controller.Dispose();
    }

    private void SetupReCaptcha(ReCaptchaVerificationResult result)
    {
        _reCaptchaMock
            .Setup(r => r.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private void SetupCheckbox(ReCaptchaVerificationResult result)
    {
        _reCaptchaMock
            .Setup(r => r.VerifyCheckboxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private void SetupEmailResponse(HttpStatusCode statusCode)
    {
        _emailSenderMock
            .Setup(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SendGrid.Response(statusCode, null, null));
    }

    private void VerifyEmailSent(Times times)
    {
        _emailSenderMock.Verify(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            times);
    }

    private static SelfReferral ValidSelfReferral() => new()
    {
        ReferringFor = ReferralChoices.FamilyMemberOrFriend,
        Purpose = "Basic needs",
        BestTimesToContact = "Evenings",
        HowHeard = "Friend or relative",
        Referrer = new ReferralPerson
        {
            FirstName = "Jamie", LastName = "Rivera", MobileNumber = "253-555-0100", OkToText = "No",
            Email = "jamie@example.com", ZipCode = "98418"
        },
        FamilyMember = new ReferralPerson
        {
            FirstName = "Chris", LastName = "Rivera", DateOfBirth = new DateTime(1980, 1, 2), Diagnosis = "Yes",
            Race = "Unknown", PrimaryLanguage = "English", CountryOfOrigin = "United States"
        },
        CaringWithoutPay = "Yes",
        Situation = "Chris needs help finding housing.",
        ReCaptchaToken = "token"
    };

    private static ProfessionalReferral ValidProfessionalReferral() => new()
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
        HasConsent = "No",
        ReCaptchaToken = "token"
    };

    [TestMethod]
    public async Task Self_Valid_SendsEmailAndRedirectsToThanks()
    {
        IActionResult result = await _controller.Self(ValidSelfReferral(), CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual(nameof(GetHelpController.Thanks), redirect.ActionName);
        VerifyEmailSent(Times.Once());
        _reCaptchaMock.Verify(r => r.VerifyAsync("token", GetHelpController.SelfReCaptchaAction, It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task Professional_Valid_UsesProfessionalReCaptchaAction()
    {
        IActionResult result = await _controller.Professional(ValidProfessionalReferral(), CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        VerifyEmailSent(Times.Once());
        _reCaptchaMock.Verify(r => r.VerifyAsync("token", GetHelpController.ProfessionalReCaptchaAction, It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task Self_ReCaptchaFailed_ShowsFormWithErrorAndDoesNotSend()
    {
        SetupReCaptcha(ReCaptchaVerificationResult.Failed);
        SelfReferral referral = ValidSelfReferral();

        IActionResult result = await _controller.Self(referral, CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result;
        Assert.AreSame(referral, view.Model);
        Assert.IsFalse(_controller.ModelState.IsValid);
        VerifyEmailSent(Times.Never());
    }

    [TestMethod]
    public async Task Self_ReCaptchaChallengeRequired_ShowsFormWithCheckboxAndDoesNotSend()
    {
        SetupReCaptcha(ReCaptchaVerificationResult.ChallengeRequired);
        SelfReferral referral = ValidSelfReferral();

        IActionResult result = await _controller.Self(referral, CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result);
        var view = (ViewResult)result;
        Assert.AreSame(referral, view.Model);
        Assert.AreEqual(true, view.ViewData[GetHelpController.ShowReCaptchaCheckboxKey]);
        Assert.IsTrue(_controller.ModelState[string.Empty]!.Errors.Count > 0);
        VerifyEmailSent(Times.Never());
    }

    [TestMethod]
    public async Task Self_ReCaptchaFailed_DoesNotShowCheckbox()
    {
        SetupReCaptcha(ReCaptchaVerificationResult.Failed);

        IActionResult result = await _controller.Self(ValidSelfReferral(), CancellationToken.None);

        var view = (ViewResult)result;
        Assert.IsNull(view.ViewData[GetHelpController.ShowReCaptchaCheckboxKey]);
    }

    [TestMethod]
    public async Task Self_CheckboxToken_VerifiesCheckboxInsteadOfScoreAndSends()
    {
        SetupCheckbox(ReCaptchaVerificationResult.Passed);
        SelfReferral referral = ValidSelfReferral();
        referral.ReCaptchaCheckboxToken = "checkbox-token";

        IActionResult result = await _controller.Self(referral, CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        VerifyEmailSent(Times.Once());
        _reCaptchaMock.Verify(r => r.VerifyCheckboxAsync("checkbox-token", It.IsAny<CancellationToken>()), Times.Once());
        _reCaptchaMock.Verify(r => r.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestMethod]
    public async Task Professional_CheckboxNotVerified_ShowsCheckboxAgain()
    {
        SetupCheckbox(ReCaptchaVerificationResult.ChallengeRequired);
        ProfessionalReferral referral = ValidProfessionalReferral();
        referral.ReCaptchaCheckboxToken = "expired-token";

        IActionResult result = await _controller.Professional(referral, CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(true, ((ViewResult)result).ViewData[GetHelpController.ShowReCaptchaCheckboxKey]);
        VerifyEmailSent(Times.Never());
    }

    [TestMethod]
    public async Task Self_CheckboxTokenRejected_DoesNotSend()
    {
        // e.g. a forged checkbox token when the checkbox isn't configured
        SetupCheckbox(ReCaptchaVerificationResult.Failed);
        SelfReferral referral = ValidSelfReferral();
        referral.ReCaptchaCheckboxToken = "forged-token";

        IActionResult result = await _controller.Self(referral, CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result);
        VerifyEmailSent(Times.Never());
    }

    [TestMethod]
    public async Task Self_ReCaptchaUnavailable_StillSends()
    {
        SetupReCaptcha(ReCaptchaVerificationResult.Unavailable);

        IActionResult result = await _controller.Self(ValidSelfReferral(), CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        VerifyEmailSent(Times.Once());
    }

    [TestMethod]
    public async Task Self_EmailFails_ShowsFormWithError()
    {
        SetupEmailResponse(HttpStatusCode.InternalServerError);

        IActionResult result = await _controller.Self(ValidSelfReferral(), CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsTrue(_controller.ModelState[string.Empty]!.Errors.Count > 0);
    }

    [TestMethod]
    public async Task Self_InvalidModel_ListsEveryProblemAndDoesNotSend()
    {
        // MVC skips IValidatableObject when a field is invalid; the controller still reports those problems
        SelfReferral referral = ValidSelfReferral();
        referral.Situation = null;
        referral.Referrer.Email = null;
        referral.CaringWithoutPay = null;
        _controller.ModelState.AddModelError(nameof(SelfReferral.Situation), "Information about your situation is required.");

        IActionResult result = await _controller.Self(referral, CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(1, _controller.ModelState[nameof(SelfReferral.Situation)]!.Errors.Count);
        Assert.IsTrue(_controller.ModelState["Referrer.Email"]!.Errors.Count > 0);
        Assert.IsTrue(_controller.ModelState[nameof(SelfReferral.CaringWithoutPay)]!.Errors.Count > 0);
        VerifyEmailSent(Times.Never());
        _reCaptchaMock.Verify(r => r.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestMethod]
    public async Task Self_InvalidModel_DoesNotDuplicateExistingErrors()
    {
        SelfReferral referral = ValidSelfReferral();
        referral.Referrer.Email = null;
        _controller.ModelState.AddModelError("Referrer.Email", "Your email is required.");

        await _controller.Self(referral, CancellationToken.None);

        Assert.AreEqual(1, _controller.ModelState["Referrer.Email"]!.Errors.Count);
    }

    [TestMethod]
    public async Task Self_ErrorInHiddenSection_IsIgnored()
    {
        // e.g. a mistyped email in the child's section before switching to "Family member or friend"
        _controller.ModelState.AddModelError("Child.Email", "Please enter a valid email address.");
        _controller.ModelState.AddModelError("Self.ZipCode", "Please enter a 5 digit ZIP code.");

        IActionResult result = await _controller.Self(ValidSelfReferral(), CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        VerifyEmailSent(Times.Once());
    }

    [TestMethod]
    public async Task Professional_WithoutConsent_IgnoresParentErrors()
    {
        _controller.ModelState.AddModelError("Parent.Email", "Please enter a valid email address.");

        IActionResult result = await _controller.Professional(ValidProfessionalReferral(), CancellationToken.None);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
    }
}
