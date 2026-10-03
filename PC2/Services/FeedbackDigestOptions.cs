using System.ComponentModel.DataAnnotations;

namespace PC2.Services;

/// <summary>
/// Weekly Resource Guide feedback digest email settings, bound from the "FeedbackDigest" configuration section.
/// </summary>
public class FeedbackDigestOptions
{
    public const string SectionName = "FeedbackDigest";

    /// <summary>
    /// When false the digest is never sent automatically. Admins can still send it manually.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The address the digest is sent to. When blank, the PC2Email address is used.
    /// </summary>
    public string? Recipient { get; set; }

    /// <summary>
    /// The day of the week the digest is sent.
    /// </summary>
    public DayOfWeek SendDay { get; set; } = DayOfWeek.Sunday;

    /// <summary>
    /// The hour (0-23, in <see cref="TimeZone"/>) the digest is sent on <see cref="SendDay"/>.
    /// </summary>
    [Range(0, 23)]
    public int SendHour { get; set; } = 8;

    /// <summary>
    /// The time zone used for the send schedule and for dates shown in the email and on the website.
    /// </summary>
    [Required]
    public string TimeZone { get; set; } = "America/Los_Angeles";

    /// <summary>
    /// How often the background service checks whether the digest is due.
    /// </summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The public URL of the website, e.g. https://www.example.org. When set, the email links to the
    /// feedback management page.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    public TimeZoneInfo GetTimeZoneInfo() => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);

    public bool IsTimeZoneValid()
    {
        try
        {
            GetTimeZoneInfo();
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
