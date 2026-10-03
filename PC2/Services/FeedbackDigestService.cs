using System.Net;
using System.Text;
using IdentityLogin.Models;
using Microsoft.Extensions.Options;
using PC2.Data;
using PC2.Models;

namespace PC2.Services;

/// <summary>
/// The outcome of an attempt to send the feedback digest email.
/// </summary>
public enum FeedbackDigestStatus
{
    /// <summary>The digest was emailed and the included feedback was marked as sent.</summary>
    Sent,

    /// <summary>There is no feedback that has not already been emailed or reviewed.</summary>
    NothingToSend,

    /// <summary>The digest is not due yet.</summary>
    NotDue,

    /// <summary>The email could not be sent. The feedback will be included in the next attempt.</summary>
    Failed
}

public record FeedbackDigestResult(FeedbackDigestStatus Status, int FeedbackCount = 0);

/// <summary>
/// Gathers Resource Guide feedback that has not been emailed or reviewed yet and emails it as a single digest.
/// </summary>
public class FeedbackDigestService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly FeedbackDigestOptions _options;
    private readonly IConfiguration _config;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FeedbackDigestService> _logger;

    public FeedbackDigestService(ApplicationDbContext context, IEmailSender emailSender,
        IOptions<FeedbackDigestOptions> options, IConfiguration config, TimeProvider timeProvider,
        ILogger<FeedbackDigestService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _options = options.Value;
        _config = config;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Sends the digest if it is due. The digest is due once the scheduled send time has passed and there is
    /// feedback submitted before that time which has not been emailed or reviewed. Because sending marks the
    /// feedback as emailed, this sends at most once per week, and catches up if the scheduled time was missed
    /// (e.g. the app was not running).
    /// </summary>
    public async Task<FeedbackDigestResult> SendIfDueAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime scheduledSendUtc = GetMostRecentScheduledSendUtc(utcNow, _options);

        List<Feedback> unhandled = await FeedbackDB.GetUnhandledAsync(_context);
        if (unhandled.Count == 0)
        {
            return new FeedbackDigestResult(FeedbackDigestStatus.NothingToSend);
        }

        if (!unhandled.Any(f => f.SubmittedAt < scheduledSendUtc))
        {
            return new FeedbackDigestResult(FeedbackDigestStatus.NotDue);
        }

        return await SendAsync(unhandled, cancellationToken);
    }

    /// <summary>
    /// Sends the digest now with all feedback that has not been emailed or reviewed, regardless of the schedule.
    /// </summary>
    public async Task<FeedbackDigestResult> SendNowAsync(CancellationToken cancellationToken = default)
    {
        List<Feedback> unhandled = await FeedbackDB.GetUnhandledAsync(_context);
        if (unhandled.Count == 0)
        {
            return new FeedbackDigestResult(FeedbackDigestStatus.NothingToSend);
        }

        return await SendAsync(unhandled, cancellationToken);
    }

    private async Task<FeedbackDigestResult> SendAsync(List<Feedback> feedback, CancellationToken cancellationToken)
    {
        string? recipient = string.IsNullOrWhiteSpace(_options.Recipient)
            ? _config.GetSection("PC2Email").Value
            : _options.Recipient;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogError("Feedback digest not sent: no recipient is configured. Set FeedbackDigest:Recipient or PC2Email.");
            return new FeedbackDigestResult(FeedbackDigestStatus.Failed, feedback.Count);
        }

        TimeZoneInfo timeZone = _options.GetTimeZoneInfo();
        DateTime sentAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        string subject = BuildSubject(feedback, ToLocal(sentAtUtc, timeZone));
        string plainText = BuildPlainText(feedback, timeZone, _options.WebsiteUrl);
        string html = BuildHtml(feedback, timeZone, _options.WebsiteUrl);

        try
        {
            SendGrid.Response response = await _emailSender.SendHtmlEmailAsync(recipient, subject, plainText, html);
            if (!response.IsSuccessStatusCode)
            {
                string body = response.Body == null ? "" : await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Feedback digest email failed with HTTP status {StatusCode}: {Body}", response.StatusCode, body);
                return new FeedbackDigestResult(FeedbackDigestStatus.Failed, feedback.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending the feedback digest email.");
            return new FeedbackDigestResult(FeedbackDigestStatus.Failed, feedback.Count);
        }

        // Not cancellable: once the email has gone out the feedback must be marked as sent, even during shutdown
        await FeedbackDB.RecordDigestAsync(_context, feedback, recipient, sentAtUtc);
        _logger.LogInformation("Feedback digest with {Count} feedback entries sent to {Recipient}.", feedback.Count, recipient);
        return new FeedbackDigestResult(FeedbackDigestStatus.Sent, feedback.Count);
    }

    /// <summary>
    /// Gets the most recent scheduled send time (e.g. last Sunday at 8 AM Pacific) at or before
    /// <paramref name="utcNow"/>, in UTC.
    /// </summary>
    public static DateTime GetMostRecentScheduledSendUtc(DateTime utcNow, FeedbackDigestOptions options)
    {
        TimeZoneInfo timeZone = options.GetTimeZoneInfo();
        DateTime localNow = ToLocal(utcNow, timeZone);

        int daysSinceSendDay = ((int)localNow.DayOfWeek - (int)options.SendDay + 7) % 7;
        DateTime scheduledLocal = localNow.Date.AddDays(-daysSinceSendDay).AddHours(options.SendHour);
        if (scheduledLocal > localNow)
        {
            scheduledLocal = scheduledLocal.AddDays(-7);
        }

        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(scheduledLocal, DateTimeKind.Unspecified), timeZone);
    }

    /// <summary>
    /// Converts a UTC date to the given time zone
    /// </summary>
    public static DateTime ToLocal(DateTime utc, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);

    public static string BuildSubject(List<Feedback> feedback, DateTime localSentAt)
    {
        string responses = feedback.Count == 1 ? "1 response" : $"{feedback.Count} responses";
        return $"Resource Guide feedback for the week ending {localSentAt:MMMM d, yyyy} ({responses})";
    }

    public static string BuildSummary(List<Feedback> feedback)
    {
        int found = feedback.Count(f => f.IsResourceFound);
        int notFound = feedback.Count - found;
        string visitors = feedback.Count == 1 ? "1 visitor" : $"{feedback.Count} visitors";
        return $"{visitors} left feedback after searching the Resource Guide. " +
               $"{found} found what they were looking for and {notFound} did not.";
    }

    public static string BuildPlainText(List<Feedback> feedback, TimeZoneInfo timeZone, string? websiteUrl)
    {
        StringBuilder text = new();
        text.AppendLine(BuildSummary(feedback));

        List<Feedback> notFound = feedback.Where(f => !f.IsResourceFound).ToList();
        List<Feedback> foundWithComments = feedback.Where(f => f.IsResourceFound && !string.IsNullOrWhiteSpace(f.Comments)).ToList();
        int foundWithoutComments = feedback.Count(f => f.IsResourceFound && string.IsNullOrWhiteSpace(f.Comments));

        if (notFound.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"DID NOT FIND WHAT THEY WERE LOOKING FOR ({notFound.Count})");
            foreach (Feedback f in notFound)
            {
                AppendPlainTextEntry(text, f, timeZone);
            }
        }

        if (foundWithComments.Count > 0 || foundWithoutComments > 0)
        {
            text.AppendLine();
            text.AppendLine($"FOUND WHAT THEY WERE LOOKING FOR ({foundWithComments.Count + foundWithoutComments})");
            foreach (Feedback f in foundWithComments)
            {
                AppendPlainTextEntry(text, f, timeZone);
            }
            if (foundWithoutComments > 0)
            {
                text.AppendLine();
                text.AppendLine(FoundWithoutCommentsText(foundWithoutComments, foundWithComments.Count > 0));
            }
        }

        string? manageUrl = GetManageUrl(websiteUrl);
        if (manageUrl != null)
        {
            text.AppendLine();
            text.AppendLine($"View all feedback on the website: {manageUrl}");
        }

        return text.ToString();
    }

    private static void AppendPlainTextEntry(StringBuilder text, Feedback f, TimeZoneInfo timeZone)
    {
        text.AppendLine();
        text.AppendLine($"- {ToLocal(f.SubmittedAt, timeZone):ddd, MMM d h:mm tt}");
        if (!string.IsNullOrWhiteSpace(f.SearchedFor))
        {
            text.AppendLine($"  Searched for: {f.SearchedFor}");
        }
        text.AppendLine($"  Comments: {(string.IsNullOrWhiteSpace(f.Comments) ? "(none)" : f.Comments.Trim())}");
    }

    public static string BuildHtml(List<Feedback> feedback, TimeZoneInfo timeZone, string? websiteUrl)
    {
        const string green = "#5b7623";

        List<Feedback> notFound = feedback.Where(f => !f.IsResourceFound).ToList();
        List<Feedback> foundWithComments = feedback.Where(f => f.IsResourceFound && !string.IsNullOrWhiteSpace(f.Comments)).ToList();
        int foundWithoutComments = feedback.Count(f => f.IsResourceFound && string.IsNullOrWhiteSpace(f.Comments));
        int found = foundWithComments.Count + foundWithoutComments;

        StringBuilder html = new();
        html.Append("<div style=\"font-family: Arial, Helvetica, sans-serif; color: #222; max-width: 640px; margin: 0 auto;\">");
        html.Append($"<h2 style=\"color: {green}; margin-bottom: 4px;\">Weekly Resource Guide Feedback</h2>");
        html.Append($"<p style=\"margin-top: 0;\">{Encode(BuildSummary(feedback))}</p>");

        html.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse; margin: 16px 0;\"><tr>");
        html.Append(StatCell(feedback.Count, "Responses", "#333"));
        html.Append(StatCell(found, "Found it", green));
        html.Append(StatCell(notFound.Count, "Did not find it", "#b02a37"));
        html.Append("</tr></table>");

        if (notFound.Count > 0)
        {
            html.Append(SectionHeading($"Did not find what they were looking for ({notFound.Count})"));
            foreach (Feedback f in notFound)
            {
                html.Append(HtmlEntry(f, timeZone, "#b02a37"));
            }
        }

        if (found > 0)
        {
            html.Append(SectionHeading($"Found what they were looking for ({found})"));
            foreach (Feedback f in foundWithComments)
            {
                html.Append(HtmlEntry(f, timeZone, green));
            }
            if (foundWithoutComments > 0)
            {
                html.Append($"<p style=\"color: #555;\">{Encode(FoundWithoutCommentsText(foundWithoutComments, foundWithComments.Count > 0))}</p>");
            }
        }

        string? manageUrl = GetManageUrl(websiteUrl);
        if (manageUrl != null)
        {
            html.Append($"<p style=\"margin-top: 24px;\"><a href=\"{Encode(manageUrl)}\" style=\"color: {green};\">View all feedback on the website</a></p>");
        }

        html.Append("<p style=\"color: #777; font-size: 12px; margin-top: 24px;\">This email is sent automatically each week by the PC2 website. " +
                    "Feedback included here won't appear in future emails.</p>");
        html.Append("</div>");
        return html.ToString();
    }

    private static string StatCell(int value, string label, string color) =>
        "<td style=\"padding: 8px 24px 8px 0;\">" +
        $"<div style=\"font-size: 28px; font-weight: bold; color: {color};\">{value}</div>" +
        $"<div style=\"font-size: 13px; color: #555;\">{Encode(label)}</div></td>";

    private static string SectionHeading(string text) =>
        $"<h3 style=\"border-bottom: 1px solid #ddd; padding-bottom: 4px; margin-top: 24px;\">{Encode(text)}</h3>";

    private static string HtmlEntry(Feedback f, TimeZoneInfo timeZone, string accentColor)
    {
        StringBuilder html = new();
        html.Append($"<div style=\"border-left: 4px solid {accentColor}; background: #f8f9fa; padding: 8px 12px; margin: 8px 0;\">");
        html.Append($"<div style=\"font-size: 12px; color: #666;\">{Encode(ToLocal(f.SubmittedAt, timeZone).ToString("dddd, MMM d 'at' h:mm tt"))}</div>");
        if (!string.IsNullOrWhiteSpace(f.SearchedFor))
        {
            html.Append($"<div style=\"margin-top: 4px;\"><strong>Searched for:</strong> {Encode(f.SearchedFor)}</div>");
        }
        if (string.IsNullOrWhiteSpace(f.Comments))
        {
            html.Append("<div style=\"margin-top: 4px; color: #777; font-style: italic;\">No comments left</div>");
        }
        else
        {
            html.Append($"<div style=\"margin-top: 4px; white-space: pre-line;\">{Encode(f.Comments.Trim())}</div>");
        }
        html.Append("</div>");
        return html.ToString();
    }

    private static string FoundWithoutCommentsText(int count, bool anyWithComments)
    {
        string others = anyWithComments ? " other" : "";
        return count == 1
            ? $"1{others} visitor found what they were looking for and didn't leave a comment."
            : $"{count}{others} visitors found what they were looking for and didn't leave a comment.";
    }

    private static string? GetManageUrl(string? websiteUrl) =>
        string.IsNullOrWhiteSpace(websiteUrl) ? null : $"{websiteUrl.TrimEnd('/')}/Feedback";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
