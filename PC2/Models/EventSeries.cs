using System.ComponentModel.DataAnnotations;

namespace PC2.Models;

/// <summary>
/// How the dates of an <see cref="EventSeries"/> are determined
/// </summary>
public enum RecurrenceFrequency
{
    /// <summary>
    /// Repeats on one or more days of the week, every <see cref="EventSeries.Interval"/> weeks
    /// </summary>
    Weekly = 1,

    /// <summary>
    /// Repeats on the Nth weekday of the month (e.g. 2nd Tuesday), every <see cref="EventSeries.Interval"/> months.
    /// The weekday is taken from <see cref="EventSeries.StartDate"/>
    /// </summary>
    MonthlyByWeekday = 2,

    /// <summary>
    /// Repeats on the same day of the month as <see cref="EventSeries.StartDate"/>, every <see cref="EventSeries.Interval"/> months.
    /// Months without that day use their last day instead
    /// </summary>
    MonthlyByDate = 3,

    /// <summary>
    /// Dates were picked individually and do not follow a pattern
    /// </summary>
    SpecificDates = 4
}

/// <summary>
/// Days of the week an <see cref="RecurrenceFrequency.Weekly"/> series repeats on
/// </summary>
[Flags]
public enum RecurrenceDays
{
    None = 0,
    Sunday = 1 << DayOfWeek.Sunday,
    Monday = 1 << DayOfWeek.Monday,
    Tuesday = 1 << DayOfWeek.Tuesday,
    Wednesday = 1 << DayOfWeek.Wednesday,
    Thursday = 1 << DayOfWeek.Thursday,
    Friday = 1 << DayOfWeek.Friday,
    Saturday = 1 << DayOfWeek.Saturday
}

/// <summary>
/// A group of calendar events that share the same details. Each date in the series
/// is stored as its own <see cref="CalendarEvent"/> so single dates can be edited or deleted
/// </summary>
public class EventSeries
{
    /// <summary>
    /// Value used for <see cref="WeekOfMonth"/> to mean the last occurrence of the weekday in the month
    /// </summary>
    public const int LastWeekOfMonth = -1;

    [Key]
    public int EventSeriesID { get; set; }

    /// <summary>
    /// How the dates of the series are determined
    /// </summary>
    public RecurrenceFrequency Frequency { get; set; }

    /// <summary>
    /// Number of weeks or months between repeats (e.g. 2 for every other week)
    /// </summary>
    public int Interval { get; set; } = 1;

    /// <summary>
    /// Days of the week a weekly series repeats on
    /// </summary>
    public RecurrenceDays WeeklyDays { get; set; }

    /// <summary>
    /// Which week of the month a <see cref="RecurrenceFrequency.MonthlyByWeekday"/> series repeats on:
    /// 1 through 4, or <see cref="LastWeekOfMonth"/>
    /// </summary>
    public int? WeekOfMonth { get; set; }

    /// <summary>
    /// First day the series can take place
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Last day the series can take place, inclusive. Null if the series ends after
    /// <see cref="OccurrenceCount"/> dates or never ends
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Number of dates in the series. Null if the series ends on <see cref="EndDate"/> or never ends
    /// </summary>
    public int? OccurrenceCount { get; set; }

    /// <summary>
    /// For a series with no end, the last day events have been created through.
    /// Events past this date are added as time goes on
    /// </summary>
    public DateOnly? GeneratedThrough { get; set; }

    /// <summary>
    /// Start time of each event in the series
    /// </summary>
    public TimeOnly StartingTime { get; set; }

    /// <summary>
    /// End time of each event in the series
    /// </summary>
    public TimeOnly EndingTime { get; set; }

    /// <summary>
    /// Description of each event in the series
    /// </summary>
    [Required]
    public string EventDescription { get; set; } = null!;

    /// <summary>
    /// True if the series is a PC2 event
    /// </summary>
    public bool PC2Event { get; set; }

    /// <summary>
    /// True if the series is a county event
    /// </summary>
    public bool CountyEvent { get; set; }

    /// <summary>
    /// The individual dates of the series
    /// </summary>
    public List<CalendarEvent> Events { get; set; } = new();

    /// <summary>
    /// True if the series repeats with no end date or count
    /// </summary>
    public bool IsOpenEnded =>
        Frequency != RecurrenceFrequency.SpecificDates && EndDate == null && OccurrenceCount == null;
}
