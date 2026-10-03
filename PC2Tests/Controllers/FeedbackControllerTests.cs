using IdentityLogin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;
using PC2.Services;

namespace PC2Tests.Controllers;

[TestClass]
public class FeedbackControllerTests
{
    private ApplicationDbContext _context = null!;
    private Mock<IReCaptchaService> _reCaptchaMock = null!;
    private FeedbackController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;
        _context = new ApplicationDbContext(options);

        _reCaptchaMock = new Mock<IReCaptchaService>();
        SetupReCaptcha(ReCaptchaVerificationResult.Passed);

        IOptions<FeedbackDigestOptions> digestOptions = Options.Create(new FeedbackDigestOptions());
        var digestService = new FeedbackDigestService(_context, Mock.Of<IEmailSender>(), digestOptions,
            new ConfigurationBuilder().Build(), TimeProvider.System, Mock.Of<ILogger<FeedbackDigestService>>());

        _controller = new FeedbackController(_context, _reCaptchaMock.Object, digestService, digestOptions,
            TimeProvider.System, Mock.Of<ILogger<FeedbackController>>());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.TempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _controller.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SetupReCaptcha(ReCaptchaVerificationResult result)
    {
        _reCaptchaMock
            .Setup(r => r.VerifyAsync(It.IsAny<string>(), FeedbackController.ReCaptchaAction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    #region Submit Tests

    [TestMethod]
    public async Task Submit_ReCaptchaPassed_SavesFeedback()
    {
        var submission = new FeedbackSubmission
        {
            IsResourceFound = false,
            Comments = "  Looking for respite care  ",
            SearchedFor = "Service: Respite Care",
            ReCaptchaToken = "token"
        };

        IActionResult result = await _controller.Submit(submission, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result);
        Feedback saved = await _context.Feedback.SingleAsync();
        Assert.IsFalse(saved.IsResourceFound);
        Assert.AreEqual("Looking for respite care", saved.Comments);
        Assert.AreEqual("Service: Respite Care", saved.SearchedFor);
        Assert.IsNull(saved.ReviewedAt);
        Assert.IsNull(saved.FeedbackDigestId);
        _reCaptchaMock.Verify(r => r.VerifyAsync("token", FeedbackController.ReCaptchaAction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Submit_ReCaptchaFailed_ReturnsBadRequestAndDoesNotSave()
    {
        SetupReCaptcha(ReCaptchaVerificationResult.Failed);

        IActionResult result = await _controller.Submit(new FeedbackSubmission { IsResourceFound = true }, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        Assert.AreEqual(0, await _context.Feedback.CountAsync());
    }

    [TestMethod]
    public async Task Submit_ReCaptchaUnavailable_SavesFeedback()
    {
        SetupReCaptcha(ReCaptchaVerificationResult.Unavailable);

        IActionResult result = await _controller.Submit(new FeedbackSubmission { IsResourceFound = true }, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result);
        Assert.AreEqual(1, await _context.Feedback.CountAsync());
    }

    [TestMethod]
    public async Task Submit_InvalidModel_ReturnsBadRequestWithoutCheckingReCaptcha()
    {
        _controller.ModelState.AddModelError(nameof(FeedbackSubmission.IsResourceFound), "Required");

        IActionResult result = await _controller.Submit(new FeedbackSubmission(), CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        Assert.AreEqual(0, await _context.Feedback.CountAsync());
        _reCaptchaMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Submit_LongSearchDescription_IsTruncated()
    {
        var submission = new FeedbackSubmission
        {
            IsResourceFound = true,
            SearchedFor = "Agency: " + new string('a', Feedback.SearchedForMaxLength)
        };

        await _controller.Submit(submission, CancellationToken.None);

        Feedback saved = await _context.Feedback.SingleAsync();
        Assert.AreEqual(Feedback.SearchedForMaxLength, saved.SearchedFor!.Length);
    }

    [TestMethod]
    public async Task Submit_BlankComments_SavesNull()
    {
        await _controller.Submit(new FeedbackSubmission { IsResourceFound = true, Comments = "   " }, CancellationToken.None);

        Feedback saved = await _context.Feedback.SingleAsync();
        Assert.IsNull(saved.Comments);
    }

    #endregion

    #region Index Tests

    [TestMethod]
    public async Task Index_Default_ShowsOnlyNewFeedback()
    {
        await FeedbackDB.AddAsync(_context, new Feedback { SubmittedAt = DateTime.UtcNow.AddDays(-2), ReviewedAt = DateTime.UtcNow });
        await FeedbackDB.AddAsync(_context, new Feedback { SubmittedAt = DateTime.UtcNow.AddDays(-1) });

        var result = await _controller.Index() as ViewResult;

        var model = result?.Model as ManageFeedbackViewModel;
        Assert.IsNotNull(model);
        Assert.HasCount(1, model.Feedback);
        Assert.AreEqual(1, model.NewFeedbackCount);
        Assert.IsFalse(model.ShowAll);
    }

    [TestMethod]
    public async Task Index_ShowAll_ShowsAllFeedback()
    {
        await FeedbackDB.AddAsync(_context, new Feedback { SubmittedAt = DateTime.UtcNow.AddDays(-2), ReviewedAt = DateTime.UtcNow });
        await FeedbackDB.AddAsync(_context, new Feedback { SubmittedAt = DateTime.UtcNow.AddDays(-1) });

        var result = await _controller.Index(showAll: true) as ViewResult;

        var model = result?.Model as ManageFeedbackViewModel;
        Assert.IsNotNull(model);
        Assert.HasCount(2, model.Feedback);
        Assert.AreEqual(1, model.NewFeedbackCount);
    }

    #endregion

    #region MarkReviewed Tests

    [TestMethod]
    public async Task MarkReviewed_ExistingFeedback_MarksReviewedAndRedirects()
    {
        var feedback = new Feedback { SubmittedAt = DateTime.UtcNow };
        await FeedbackDB.AddAsync(_context, feedback);

        IActionResult result = await _controller.MarkReviewed(feedback.FeedbackId);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.IsNotNull(feedback.ReviewedAt);
    }

    [TestMethod]
    public async Task MarkReviewed_NotFound_ReturnsNotFound()
    {
        IActionResult result = await _controller.MarkReviewed(999);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    #endregion
}
