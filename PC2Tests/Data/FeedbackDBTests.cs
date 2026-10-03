using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class FeedbackDBTests
{
    private ApplicationDbContext _context = null!;

    private static readonly DateTime Now = new(2026, 10, 4, 15, 0, 0, DateTimeKind.Utc);

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;

        _context = new ApplicationDbContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<Feedback> AddFeedbackAsync(DateTime submittedAt, DateTime? reviewedAt = null, FeedbackDigest? digest = null)
    {
        var feedback = new Feedback
        {
            IsResourceFound = true,
            SubmittedAt = submittedAt,
            ReviewedAt = reviewedAt,
            FeedbackDigest = digest
        };
        await FeedbackDB.AddAsync(_context, feedback);
        return feedback;
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsNewestFirstWithDigest()
    {
        var digest = new FeedbackDigest { SentAt = Now, SentTo = "info@example.org" };
        await AddFeedbackAsync(Now.AddDays(-2), digest: digest);
        await AddFeedbackAsync(Now.AddDays(-1));

        List<Feedback> result = await FeedbackDB.GetAllAsync(_context);

        Assert.HasCount(2, result);
        Assert.AreEqual(Now.AddDays(-1), result[0].SubmittedAt);
        Assert.IsNotNull(result[1].FeedbackDigest);
    }

    [TestMethod]
    public async Task GetUnhandledAsync_ExcludesEmailedAndReviewedFeedback()
    {
        var digest = new FeedbackDigest { SentAt = Now, SentTo = "info@example.org" };
        await AddFeedbackAsync(Now.AddDays(-3), digest: digest);
        await AddFeedbackAsync(Now.AddDays(-2), reviewedAt: Now);
        Feedback newer = await AddFeedbackAsync(Now.AddDays(-1));
        Feedback older = await AddFeedbackAsync(Now.AddDays(-4));

        List<Feedback> result = await FeedbackDB.GetUnhandledAsync(_context);

        CollectionAssert.AreEqual(new[] { older.FeedbackId, newer.FeedbackId }, result.Select(f => f.FeedbackId).ToArray());
        Assert.AreEqual(2, await FeedbackDB.GetUnhandledCountAsync(_context));
    }

    [TestMethod]
    public async Task MarkReviewedAsync_SetsReviewedAt()
    {
        Feedback feedback = await AddFeedbackAsync(Now.AddDays(-1));

        bool found = await FeedbackDB.MarkReviewedAsync(_context, feedback.FeedbackId, Now);

        Assert.IsTrue(found);
        Assert.AreEqual(Now, feedback.ReviewedAt);
    }

    [TestMethod]
    public async Task MarkReviewedAsync_AlreadyReviewed_KeepsOriginalReviewedAt()
    {
        Feedback feedback = await AddFeedbackAsync(Now.AddDays(-2), reviewedAt: Now.AddDays(-1));

        await FeedbackDB.MarkReviewedAsync(_context, feedback.FeedbackId, Now);

        Assert.AreEqual(Now.AddDays(-1), feedback.ReviewedAt);
    }

    [TestMethod]
    public async Task MarkReviewedAsync_NotFound_ReturnsFalse()
    {
        bool found = await FeedbackDB.MarkReviewedAsync(_context, 999, Now);

        Assert.IsFalse(found);
    }

    [TestMethod]
    public async Task MarkAllUnhandledReviewedAsync_OnlyMarksNewFeedback()
    {
        var digest = new FeedbackDigest { SentAt = Now.AddDays(-7), SentTo = "info@example.org" };
        Feedback emailed = await AddFeedbackAsync(Now.AddDays(-8), digest: digest);
        Feedback first = await AddFeedbackAsync(Now.AddDays(-2));
        Feedback second = await AddFeedbackAsync(Now.AddDays(-1));

        int count = await FeedbackDB.MarkAllUnhandledReviewedAsync(_context, Now);

        Assert.AreEqual(2, count);
        Assert.IsNull(emailed.ReviewedAt);
        Assert.AreEqual(Now, first.ReviewedAt);
        Assert.AreEqual(Now, second.ReviewedAt);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesFeedback()
    {
        Feedback feedback = await AddFeedbackAsync(Now);

        await FeedbackDB.DeleteAsync(_context, feedback.FeedbackId);

        Assert.AreEqual(0, await _context.Feedback.CountAsync());
    }

    [TestMethod]
    public async Task RecordDigestAsync_LinksFeedbackToDigest()
    {
        Feedback feedback = await AddFeedbackAsync(Now.AddDays(-1));

        FeedbackDigest digest = await FeedbackDB.RecordDigestAsync(_context, new List<Feedback> { feedback }, "info@example.org", Now);

        Assert.AreEqual(digest.FeedbackDigestId, feedback.FeedbackDigestId);
        Assert.AreEqual(digest.FeedbackDigestId, (await FeedbackDB.GetLastDigestAsync(_context))?.FeedbackDigestId);
    }
}
