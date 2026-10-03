using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data;

public class FeedbackDB
{
    /// <summary>
    /// Adds feedback to the database
    /// </summary>
    /// <param name="context"></param>
    /// <param name="feedback"></param>
    /// <returns></returns>
    public static async Task AddAsync(ApplicationDbContext context, Feedback feedback)
    {
        context.Feedback.Add(feedback);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all feedback with the digest it was emailed in, newest first
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<List<Feedback>> GetAllAsync(ApplicationDbContext context)
    {
        return await (from f in context.Feedback.Include(f => f.FeedbackDigest)
                      orderby f.SubmittedAt descending
                      select f).ToListAsync();
    }

    /// <summary>
    /// Gets feedback that has not been emailed in a digest or marked as reviewed, oldest first
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<List<Feedback>> GetUnhandledAsync(ApplicationDbContext context)
    {
        return await (from f in context.Feedback
                      where f.FeedbackDigestId == null && f.ReviewedAt == null
                      orderby f.SubmittedAt
                      select f).ToListAsync();
    }

    /// <summary>
    /// Gets the number of feedback entries that have not been emailed in a digest or marked as reviewed
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<int> GetUnhandledCountAsync(ApplicationDbContext context)
    {
        return await context.Feedback.CountAsync(f => f.FeedbackDigestId == null && f.ReviewedAt == null);
    }

    /// <summary>
    /// Gets feedback based on ID
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static async Task<Feedback?> GetFeedbackAsync(ApplicationDbContext context, int id)
    {
        return await context.Feedback.FindAsync(id);
    }

    /// <summary>
    /// Marks feedback as reviewed so it is not included in the next digest email
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <param name="reviewedAt">When the feedback was reviewed, in UTC</param>
    /// <returns>True if the feedback was found</returns>
    public static async Task<bool> MarkReviewedAsync(ApplicationDbContext context, int id, DateTime reviewedAt)
    {
        Feedback? feedback = await context.Feedback.FindAsync(id);
        if (feedback == null)
        {
            return false;
        }

        feedback.ReviewedAt ??= reviewedAt;
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Marks all feedback that has not been emailed or reviewed as reviewed
    /// </summary>
    /// <param name="context"></param>
    /// <param name="reviewedAt">When the feedback was reviewed, in UTC</param>
    /// <returns>The number of feedback entries marked as reviewed</returns>
    public static async Task<int> MarkAllUnhandledReviewedAsync(ApplicationDbContext context, DateTime reviewedAt)
    {
        List<Feedback> unhandled = await GetUnhandledAsync(context);
        foreach (Feedback feedback in unhandled)
        {
            feedback.ReviewedAt = reviewedAt;
        }

        await context.SaveChangesAsync();
        return unhandled.Count;
    }

    /// <summary>
    /// Deletes feedback based on ID
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static async Task DeleteAsync(ApplicationDbContext context, int id)
    {
        Feedback? feedback = await context.Feedback.FindAsync(id);
        if (feedback != null)
        {
            context.Feedback.Remove(feedback);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Records a digest email and links the feedback it included so it is not emailed again
    /// </summary>
    /// <param name="context"></param>
    /// <param name="feedback">The feedback included in the digest</param>
    /// <param name="sentTo">The email address the digest was sent to</param>
    /// <param name="sentAt">When the digest was sent, in UTC</param>
    /// <returns>The saved digest</returns>
    public static async Task<FeedbackDigest> RecordDigestAsync(ApplicationDbContext context,
        List<Feedback> feedback, string sentTo, DateTime sentAt)
    {
        FeedbackDigest digest = new()
        {
            SentAt = sentAt,
            SentTo = sentTo,
            Feedback = feedback
        };

        context.FeedbackDigests.Add(digest);
        await context.SaveChangesAsync();
        return digest;
    }

    /// <summary>
    /// Gets the most recently sent digest email, or null if none have been sent
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<FeedbackDigest?> GetLastDigestAsync(ApplicationDbContext context)
    {
        return await (from d in context.FeedbackDigests
                      orderby d.SentAt descending
                      select d).FirstOrDefaultAsync();
    }
}
