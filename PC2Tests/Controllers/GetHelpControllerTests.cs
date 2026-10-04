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
        ReferringFor = "Myself",
        FirstName = "Jamie",
        LastName = "Rivera",
        Email = "jamie@example.com",
        Purposes = ["Employment"],
        ReCaptchaToken = "token"
    };

    private static ProfessionalReferral ValidProfessionalReferral() => new()
    {
        OrganizationName = "Tacoma Public Schools",
        FirstName = "Pat",
        LastName = "Lee",
        RoleOrRelationship = "Special education teacher",
        Email = "pat@example.org",
        ContactName = "Alex Morgan",
        ContactPhone = "253-555-0199",
        Purposes = ["Transition to adulthood"],
        HasConsent = true,
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
        referral.FirstName = null;
        referral.Email = null;
        referral.Purposes = [];
        _controller.ModelState.AddModelError(nameof(SelfReferral.FirstName), "Please enter your first name.");

        IActionResult result = await _controller.Self(referral, CancellationToken.None);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(1, _controller.ModelState[nameof(SelfReferral.FirstName)]!.Errors.Count);
        Assert.IsTrue(_controller.ModelState[nameof(SelfReferral.Phone)]!.Errors.Count > 0);
        Assert.IsTrue(_controller.ModelState[nameof(ReferralSubmission.Purposes)]!.Errors.Count > 0);
        VerifyEmailSent(Times.Never());
        _reCaptchaMock.Verify(r => r.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestMethod]
    public async Task Self_InvalidModel_DoesNotDuplicateExistingErrors()
    {
        SelfReferral referral = ValidSelfReferral();
        referral.Email = null;
        _controller.ModelState.AddModelError(nameof(SelfReferral.Phone), "Please enter a valid phone number.");

        await _controller.Self(referral, CancellationToken.None);

        Assert.AreEqual(1, _controller.ModelState[nameof(SelfReferral.Phone)]!.Errors.Count);
    }
}
