namespace PC2.Models;

/// <summary>
/// PC2 is in Tacoma, so dates on the site (event dates, job closing dates) are Pacific time dates.
/// The server's clock is UTC on Azure, so DateTime.Today rolls over to tomorrow at 4–5 PM Pacific.
/// </summary>
public static class PacificTime
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

    /// <summary>
    /// Today's date in Pacific time. Worked out on each call, so it changes at midnight Pacific.
    /// </summary>
    /// <param name="timeProvider">The clock to use; the system clock when null</param>
    public static DateOnly Today(TimeProvider? timeProvider = null)
    {
        DateTimeOffset now = (timeProvider ?? TimeProvider.System).GetUtcNow();
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZone).DateTime);
    }
}
