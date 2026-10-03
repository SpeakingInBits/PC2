using IdentityLogin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PC2.Data;
using PC2.Models;
using PC2.Services;
using System.Net;

namespace PC2Tests.Services;

[TestClass]
public class FeedbackDigestServiceTests
{
    private const string PC2Email = "info@example.org";

    // Sunday, October 4, 2026 at 8 AM Pacific Daylight Time (UTC-7)
    private static readonly DateTime SundayEightAmUtc = new(2026, 10, 4, 15, 0, 0, DateTimeKind.Utc);

    private ApplicationDbContext _context = null!;
    private Mock<IEmailSender> _emailSenderMock = null!;
    private FakeTimeProvider _timeProvider = null!;
    private FeedbackDigestOptions _options = null!;

    /// <summary>
    /// A TimeProvider whose current time is set by the test
    /// </summary>
    private class FakeTimeProvider : TimeProvider
    {
        public DateTime UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;
        _context = new ApplicationDbContext(options);

        _emailSenderMock = new Mock<IEmailSender>();
        SetupEmailResponse(HttpStatusCode.Accepted);

        _timeProvider = new FakeTimeProvider { UtcNow = SundayEightAmUtc.AddHours(1) };
        _options = new FeedbackDigestOptions();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private FeedbackDigestService CreateService()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PC2Email"] = PC2Email })
            .Build();

        return new FeedbackDigestService(_context, _emailSenderMock.Object, Options.Create(_options), config,
            _timeProvider, Mock.Of<ILogger<FeedbackDigestService>>());
    }

    private void SetupEmailResponse(HttpStatusCode statusCode)
    {
        _emailSenderMock
            .Setup(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SendGrid.Response(statusCode, null, null));
    }

    private async Task<Feedback> AddFeedbackAsync(DateTime submittedAt, bool isResourceFound = false,
        string? comments = null, string? searchedFor = null, DateTime? reviewedAt = null)
    {
        var feedback = new Feedback
        {
            IsResourceFound = isResourceFound,
            Comments = comments,
            SearchedFor = searchedFor,
            SubmittedAt = submittedAt,
            ReviewedAt = reviewedAt
        };
        await FeedbackDB.AddAsync(_context, feedback);
        return feedback;
    }

    #region GetMostRecentScheduledSendUtc Tests

    [TestMethod]
    public void GetMostRecentScheduledSendUtc_SundayAfterSendHour_ReturnsToday()
    {
        DateTime result = FeedbackDigestService.GetMostRecentScheduledSendUtc(SundayEightAmUtc.AddHours(1), new FeedbackDigestOptions());

        Assert.AreEqual(SundayEightAmUtc, result);
    }

    [TestMethod]
    public void GetMostRecentScheduledSendUtc_ExactlySendTime_ReturnsSendTime()
    {
        DateTime result = FeedbackDigestService.GetMostRecentScheduledSendUtc(SundayEightAmUtc, new FeedbackDigestOptions());

        Assert.AreEqual(SundayEightAmUtc, result);
    }

    [TestMethod]
    public void GetMostRecentScheduledSendUtc_SundayBeforeSendHour_ReturnsPreviousSunday()
    {
        DateTime result = FeedbackDigestService.GetMostRecentScheduledSendUtc(SundayEightAmUtc.AddHours(-1), new FeedbackDigestOptions());

        Assert.AreEqual(SundayEightAmUtc.AddDays(-7), result);
    }

    [TestMethod]
    public void GetMostRecentScheduledSendUtc_MidWeek_ReturnsPreviousSunday()
    {
        // Wednesday, October 7, 2026
        DateTime result = FeedbackDigestService.GetMostRecentScheduledSendUtc(SundayEightAmUtc.AddDays(3), new FeedbackDigestOptions());

        Assert.AreEqual(SundayEightAmUtc, result);
    }

    [TestMethod]
    public void GetMostRecentScheduledSendUtc_SaturdayEveningPacific_ReturnsPreviousSunday()
    {
        // Saturday, October 10 at 11 PM Pacific is already Sunday in UTC
        DateTime saturdayNightUtc = new(2026, 10, 11, 6, 0, 0, DateTimeKind.Utc);

        DateTime result = FeedbackDigestService.GetMostRecentScheduledSendUtc(saturdayNightUtc, new FeedbackDigestOptions());

        Assert.AreEqual(SundayEightAmUtc, result);
    }

    [TestMethod]
    public void GetMostRecentScheduledSendUtc_StandardTime_UsesPacificStandardOffset()
    {
        // Sunday, December 6, 2026 at noon Pacific Standard Time (UTC-8)
        DateTime now = new(2026, 12, 6, 20, 0, 0, DateTimeKind.Utc);

        DateTime result = FeedbackDigestService.GetMostRecentScheduledSendUtc(now, new FeedbackDigestOptions());

        Assert.AreEqual(new DateTime(2026, 12, 6, 16, 0, 0, DateTimeKind.Utc), result);
    }

    [TestMethod]
    public void GetMostRecentScheduledSendUtc_CustomDayAndHour_UsesSettings()
    {
        var options = new FeedbackDigestOptions { SendDay = DayOfWeek.Monday, SendHour = 17 };

        // Wednesday, October 7, 2026
        DateTime result = FeedbackDigestService.GetMostRecentScheduledSendUtc(SundayEightAmUtc.AddDays(3), options);

        // Monday, October 5 at 5 PM Pacific Daylight Time
        Assert.AreEqual(new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc), result);
    }

    #endregion

    #region SendIfDueAsync Tests

    [TestMethod]
    public async Task SendIfDueAsync_NoFeedback_ReturnsNothingToSend()
    {
        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.NothingToSend, result.Status);
        _emailSenderMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendIfDueAsync_OnlyFeedbackAfterScheduledTime_ReturnsNotDue()
    {
        await AddFeedbackAsync(SundayEightAmUtc.AddMinutes(30));

        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.NotDue, result.Status);
        _emailSenderMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendIfDueAsync_FeedbackBeforeScheduledTime_SendsAllNewFeedback()
    {
        Feedback lastWeek = await AddFeedbackAsync(SundayEightAmUtc.AddDays(-2));
        Feedback thisMorning = await AddFeedbackAsync(SundayEightAmUtc.AddMinutes(30));

        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.Sent, result.Status);
        Assert.AreEqual(2, result.FeedbackCount);
        _emailSenderMock.Verify(s => s.SendHtmlEmailAsync(PC2Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        FeedbackDigest digest = await _context.FeedbackDigests.SingleAsync();
        Assert.AreEqual(PC2Email, digest.SentTo);
        Assert.AreEqual(_timeProvider.UtcNow, digest.SentAt);
        Assert.AreEqual(digest.FeedbackDigestId, lastWeek.FeedbackDigestId);
        Assert.AreEqual(digest.FeedbackDigestId, thisMorning.FeedbackDigestId);
    }

    [TestMethod]
    public async Task SendIfDueAsync_AfterSending_DoesNotSendAgainUntilNextWeek()
    {
        await AddFeedbackAsync(SundayEightAmUtc.AddDays(-2));
        FeedbackDigestService service = CreateService();
        await service.SendIfDueAsync();

        // Feedback arrives later on Sunday and during the week
        await AddFeedbackAsync(SundayEightAmUtc.AddHours(3));
        _timeProvider.UtcNow = SundayEightAmUtc.AddHours(4);
        FeedbackDigestResult sameDay = await service.SendIfDueAsync();

        _timeProvider.UtcNow = SundayEightAmUtc.AddDays(6);
        FeedbackDigestResult saturday = await service.SendIfDueAsync();

        _timeProvider.UtcNow = SundayEightAmUtc.AddDays(7);
        FeedbackDigestResult nextSunday = await service.SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.NotDue, sameDay.Status);
        Assert.AreEqual(FeedbackDigestStatus.NotDue, saturday.Status);
        Assert.AreEqual(FeedbackDigestStatus.Sent, nextSunday.Status);
        Assert.AreEqual(1, nextSunday.FeedbackCount);
        Assert.AreEqual(2, await _context.FeedbackDigests.CountAsync());
    }

    [TestMethod]
    public async Task SendIfDueAsync_ScheduledTimeMissed_SendsWhenNextChecked()
    {
        await AddFeedbackAsync(SundayEightAmUtc.AddDays(-2));

        // The app was not running on Sunday morning
        _timeProvider.UtcNow = SundayEightAmUtc.AddDays(1);
        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.Sent, result.Status);
    }

    [TestMethod]
    public async Task SendIfDueAsync_ReviewedFeedback_IsNotEmailed()
    {
        Feedback reviewed = await AddFeedbackAsync(SundayEightAmUtc.AddDays(-3), reviewedAt: SundayEightAmUtc.AddDays(-1));
        Feedback newFeedback = await AddFeedbackAsync(SundayEightAmUtc.AddDays(-2));

        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(1, result.FeedbackCount);
        Assert.IsNull(reviewed.FeedbackDigestId);
        Assert.IsNotNull(newFeedback.FeedbackDigestId);
    }

    [TestMethod]
    public async Task SendIfDueAsync_OnlyReviewedFeedback_ReturnsNothingToSend()
    {
        await AddFeedbackAsync(SundayEightAmUtc.AddDays(-3), reviewedAt: SundayEightAmUtc.AddDays(-1));

        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.NothingToSend, result.Status);
        _emailSenderMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SendIfDueAsync_EmailRejected_ReturnsFailedAndKeepsFeedbackNew()
    {
        SetupEmailResponse(HttpStatusCode.Unauthorized);
        Feedback feedback = await AddFeedbackAsync(SundayEightAmUtc.AddDays(-2));

        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.Failed, result.Status);
        Assert.IsNull(feedback.FeedbackDigestId);
        Assert.AreEqual(0, await _context.FeedbackDigests.CountAsync());
    }

    [TestMethod]
    public async Task SendIfDueAsync_EmailThrows_ReturnsFailedAndKeepsFeedbackNew()
    {
        _emailSenderMock
            .Setup(s => s.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("SendGrid is down"));
        Feedback feedback = await AddFeedbackAsync(SundayEightAmUtc.AddDays(-2));

        FeedbackDigestResult result = await CreateService().SendIfDueAsync();

        Assert.AreEqual(FeedbackDigestStatus.Failed, result.Status);
        Assert.IsNull(feedback.FeedbackDigestId);
    }

    [TestMethod]
    public async Task SendIfDueAsync_RecipientConfigured_SendsToRecipient()
    {
        _options.Recipient = "feedback@example.org";
        await AddFeedbackAsync(SundayEightAmUtc.AddDays(-2));

        await CreateService().SendIfDueAsync();

        _emailSenderMock.Verify(s => s.SendHtmlEmailAsync("feedback@example.org", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region SendNowAsync Tests

    [TestMethod]
    public async Task SendNowAsync_FeedbackAfterScheduledTime_SendsImmediately()
    {
        Feedback feedback = await AddFeedbackAsync(SundayEightAmUtc.AddMinutes(30));

        FeedbackDigestResult result = await CreateService().SendNowAsync();

        Assert.AreEqual(FeedbackDigestStatus.Sent, result.Status);
        Assert.IsNotNull(feedback.FeedbackDigestId);
    }

    [TestMethod]
    public async Task SendNowAsync_NoNewFeedback_ReturnsNothingToSend()
    {
        FeedbackDigestResult result = await CreateService().SendNowAsync();

        Assert.AreEqual(FeedbackDigestStatus.NothingToSend, result.Status);
        _emailSenderMock.VerifyNoOtherCalls();
    }

    #endregion

    #region Email Content Tests

    [TestMethod]
    public void BuildHtml_EncodesVisitorText()
    {
        var feedback = new List<Feedback>
        {
            new() { IsResourceFound = false, Comments = "<script>alert('x')</script>", SearchedFor = "City: <b>Tacoma</b>", SubmittedAt = SundayEightAmUtc }
        };

        string html = FeedbackDigestService.BuildHtml(feedback, new FeedbackDigestOptions().GetTimeZoneInfo(), null);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("City: &lt;b&gt;Tacoma&lt;/b&gt;", html);
    }

    [TestMethod]
    public void BuildPlainText_IncludesSummarySearchAndComments()
    {
        var feedback = new List<Feedback>
        {
            new() { IsResourceFound = false, Comments = "Couldn't find respite care", SearchedFor = "Service: Respite Care", SubmittedAt = SundayEightAmUtc },
            new() { IsResourceFound = true, SubmittedAt = SundayEightAmUtc },
            new() { IsResourceFound = true, SubmittedAt = SundayEightAmUtc }
        };

        string text = FeedbackDigestService.BuildPlainText(feedback, new FeedbackDigestOptions().GetTimeZoneInfo(), "https://example.org/");

        Assert.Contains("3 visitors left feedback", text);
        Assert.Contains("2 found what they were looking for and 1 did not", text);
        Assert.Contains("Searched for: Service: Respite Care", text);
        Assert.Contains("Comments: Couldn't find respite care", text);
        Assert.Contains("2 visitors found what they were looking for and didn't leave a comment", text);
        Assert.Contains("https://example.org/Feedback", text);
    }

    [TestMethod]
    public void BuildPlainText_ShowsTimesInConfiguredTimeZone()
    {
        var feedback = new List<Feedback>
        {
            new() { IsResourceFound = false, SubmittedAt = SundayEightAmUtc }
        };

        string text = FeedbackDigestService.BuildPlainText(feedback, new FeedbackDigestOptions().GetTimeZoneInfo(), null);

        Assert.Contains("Sun, Oct 4 8:00 AM", text);
    }

    [TestMethod]
    public void BuildSubject_IncludesDateAndResponseCount()
    {
        var feedback = new List<Feedback> { new() { SubmittedAt = SundayEightAmUtc } };

        string subject = FeedbackDigestService.BuildSubject(feedback, new DateTime(2026, 10, 4, 8, 0, 0));

        Assert.AreEqual("Resource Guide feedback for the week ending October 4, 2026 (1 response)", subject);
    }

    #endregion
}
