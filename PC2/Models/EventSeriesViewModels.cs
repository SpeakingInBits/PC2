using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PC2.Models;

/// <summary>
/// The "Repeats" choices on the event forms
/// </summary>
public enum RepeatOption
{
    [Display(Name = "Does not repeat")]
    None = 0,

    [Display(Name = "Weekly")]
    Weekly = 1,

    [Display(Name = "Monthly on the same weekday")]
    MonthlyByWeekday = 2,

    [Display(Name = "Monthly on the last weekday")]
    MonthlyByLastWeekday = 3,

    [Display(Name = "Monthly on the same date")]
    MonthlyByDate = 4,

    [Display(Name = "On specific dates")]
    SpecificDates = 5
}

/// <summary>
/// When a repeating event stops
/// </summary>
public enum RepeatEnd
{
    Never = 0,
    OnDate = 1,
    AfterCount = 2
}

/// <summary>
/// The "Repeats" section of the event forms. Describes the dates of a series, starting on a given date
/// </summary>
public class RecurrenceInputModel
{
    public const int MaxInterval = 12;
    public const int MaxOccurrences = 100;
    public const int MaxSpecificDates = 50;
    public const int MaxYearsAhead = 2;

    /// <summary>
    /// How the event repeats
    /// </summary>
    [Display(Name = "Repeats")]
    public RepeatOption Repeat { get; set; }

    /// <summary>
    /// Number of weeks or months between repeats
    /// </summary>
    [Display(Name = "Repeat every")]
    public int Interval { get; set; } = 1;

    /// <summary>
    /// Days of the week a weekly event repeats on
    /// </summary>
    public List<DayOfWeek> WeeklyDays { get; set; } = new();

    /// <summary>
    /// When the event stops repeating
    /// </summary>
    [Display(Name = "Ends")]
    public RepeatEnd End { get; set; }

    /// <summary>
    /// Last day the event can repeat on, used when <see cref="End"/> is <see cref="RepeatEnd.OnDate"/>
    /// </summary>
    [Display(Name = "End date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Number of times the event takes place, used when <see cref="End"/> is <see cref="RepeatEnd.AfterCount"/>
    /// </summary>
    [Display(Name = "Number of times")]
    public int? OccurrenceCount { get; set; }

    /// <summary>
    /// Dates in addition to the start date, used when <see cref="Repeat"/> is <see cref="RepeatOption.SpecificDates"/>
    /// </summary>
    public List<DateTime> SpecificDates { get; set; } = new();

    /// <summary>
    /// Dates of the pattern the admin unchecked in the preview, which will not be created
    /// </summary>
    public List<DateTime> ExcludedDates { get; set; } = new();

    /// <summary>
    /// True if the event repeats
    /// </summary>
    public bool Repeats => Repeat != RepeatOption.None;

    /// <summary>
    /// Gets the text shown for a "Repeats" choice
    /// </summary>
    public static string GetLabel(RepeatOption option)
    {
        return typeof(RepeatOption).GetField(option.ToString())?
            .GetCustomAttribute<DisplayAttribute>()?.Name ?? option.ToString();
    }

    /// <summary>
    /// Validates the recurrence for a series starting on <paramref name="startDate"/>
    /// </summary>
    /// <param name="startDate">First day of the series</param>
    /// <param name="today">The current date</param>
    /// <param name="prefix">Name of the property holding this model, used to match errors to form fields</param>
    public IEnumerable<ValidationResult> Validate(DateOnly startDate, DateOnly today, string prefix)
    {
        if (!Repeats)
        {
            yield break;
        }

        string Field(string name) => $"{prefix}.{name}";

        if (Repeat == RepeatOption.SpecificDates)
        {
            if (SpecificDates.Count == 0)
            {
                yield return new ValidationResult("Add at least one more date", new[] { Field(nameof(SpecificDates)) });
            }
            else if (SpecificDates.Count > MaxSpecificDates)
            {
                yield return new ValidationResult($"Add no more than {MaxSpecificDates} dates", new[] { Field(nameof(SpecificDates)) });
            }
            else if (SpecificDates.Any(d => DateOnly.FromDateTime(d) < today))
            {
                yield return new ValidationResult("Dates must be today or in the future", new[] { Field(nameof(SpecificDates)) });
            }
        }
        else
        {
            if (Interval < 1 || Interval > MaxInterval)
            {
                yield return new ValidationResult($"Repeat every 1 to {MaxInterval}", new[] { Field(nameof(Interval)) });
            }

            if (Repeat == RepeatOption.Weekly && WeeklyDays.Count == 0)
            {
                yield return new ValidationResult("Choose at least one day of the week", new[] { Field(nameof(WeeklyDays)) });
            }

            if (Repeat == RepeatOption.MonthlyByLastWeekday && !EventRecurrence.IsLastWeekdayOfMonth(startDate))
            {
                yield return new ValidationResult(
                    $"{startDate:MMMM d} is not the last {startDate.DayOfWeek} of the month",
                    new[] { Field(nameof(Repeat)) });
            }

            if (End == RepeatEnd.OnDate)
            {
                if (EndDate == null)
                {
                    yield return new ValidationResult("Enter the date the event stops repeating", new[] { Field(nameof(EndDate)) });
                }
                else if (DateOnly.FromDateTime(EndDate.Value) < startDate)
                {
                    yield return new ValidationResult("End date must be on or after the start date", new[] { Field(nameof(EndDate)) });
                }
                else if (DateOnly.FromDateTime(EndDate.Value) > startDate.AddYears(MaxYearsAhead))
                {
                    yield return new ValidationResult(
                        $"End date must be within {MaxYearsAhead} years of the start date. Choose \"Never\" for an event with no end",
                        new[] { Field(nameof(EndDate)) });
                }
            }
            else if (End == RepeatEnd.AfterCount && (OccurrenceCount == null || OccurrenceCount < 1 || OccurrenceCount > MaxOccurrences))
            {
                yield return new ValidationResult($"Enter a number from 1 to {MaxOccurrences}", new[] { Field(nameof(OccurrenceCount)) });
            }
        }
    }

    /// <summary>
    /// Copies the repeat pattern to a series starting on <paramref name="startDate"/>
    /// </summary>
    public void ApplyTo(EventSeries series, DateOnly startDate)
    {
        series.StartDate = startDate;
        series.Interval = Repeat == RepeatOption.SpecificDates ? 1 : Interval;
        series.WeeklyDays = RecurrenceDays.None;
        series.WeekOfMonth = null;
        series.EndDate = null;
        series.OccurrenceCount = null;

        switch (Repeat)
        {
            case RepeatOption.Weekly:
                series.Frequency = RecurrenceFrequency.Weekly;
                foreach (DayOfWeek day in WeeklyDays)
                {
                    series.WeeklyDays |= (RecurrenceDays)(1 << (int)day);
                }
                break;
            case RepeatOption.MonthlyByWeekday:
                series.Frequency = RecurrenceFrequency.MonthlyByWeekday;
                series.WeekOfMonth = EventRecurrence.DefaultWeekOfMonth(startDate);
                break;
            case RepeatOption.MonthlyByLastWeekday:
                series.Frequency = RecurrenceFrequency.MonthlyByWeekday;
                series.WeekOfMonth = EventSeries.LastWeekOfMonth;
                break;
            case RepeatOption.MonthlyByDate:
                series.Frequency = RecurrenceFrequency.MonthlyByDate;
                break;
            default:
                series.Frequency = RecurrenceFrequency.SpecificDates;
                return;
        }

        if (End == RepeatEnd.OnDate && EndDate.HasValue)
        {
            series.EndDate = DateOnly.FromDateTime(EndDate.Value);
        }
        else if (End == RepeatEnd.AfterCount)
        {
            series.OccurrenceCount = OccurrenceCount;
        }
    }

    /// <summary>
    /// Gets every date of the series starting on <paramref name="startDate"/>, including dates
    /// unchecked in the preview. Open-ended series stop <see cref="EventRecurrence.OpenEndedHorizonMonths"/> months from today
    /// </summary>
    public List<DateOnly> GetAllDates(DateOnly startDate, DateOnly today)
    {
        if (Repeat == RepeatOption.SpecificDates)
        {
            return SpecificDates.Select(DateOnly.FromDateTime)
                .Append(startDate)
                .Distinct()
                .Order()
                .ToList();
        }

        EventSeries series = new() { EventDescription = string.Empty };
        ApplyTo(series, startDate);
        DateOnly limit = EventRecurrence.GetGenerationLimit(series, today);
        return EventRecurrence.GetDates(series, startDate, limit).ToList();
    }

    /// <summary>
    /// Gets the dates events should be created on, leaving out dates unchecked in the preview
    /// </summary>
    public List<DateOnly> GetSelectedDates(DateOnly startDate, DateOnly today)
    {
        HashSet<DateOnly> excluded = GetExcludedDates();
        return GetAllDates(startDate, today).Where(d => !excluded.Contains(d)).ToList();
    }

    /// <summary>
    /// Gets the dates unchecked in the preview
    /// </summary>
    public HashSet<DateOnly> GetExcludedDates()
    {
        return ExcludedDates.Select(DateOnly.FromDateTime).ToHashSet();
    }

    /// <summary>
    /// Creates the form fields for an existing series, so it can be edited.
    /// Dates of the pattern that were deleted from the series are marked as excluded
    /// </summary>
    /// <param name="series">The series being edited</param>
    /// <param name="upcomingDates">Dates of the series that are today or later</param>
    /// <param name="startDate">The date the edited schedule starts on</param>
    /// <param name="today">The current date</param>
    public static RecurrenceInputModel FromSeries(EventSeries series, IReadOnlyCollection<DateOnly> upcomingDates,
        DateOnly startDate, DateOnly today)
    {
        RecurrenceInputModel model = new()
        {
            Interval = series.Interval,
            WeeklyDays = Enum.GetValues<DayOfWeek>()
                .Where(day => series.WeeklyDays.HasFlag((RecurrenceDays)(1 << (int)day)))
                .ToList(),
            End = series.EndDate.HasValue ? RepeatEnd.OnDate
                : series.OccurrenceCount.HasValue ? RepeatEnd.AfterCount
                : RepeatEnd.Never,
            EndDate = series.EndDate?.ToDateTime(TimeOnly.MinValue),
            // The series counts from its original start, so show how many remain from the edited start
            OccurrenceCount = series.OccurrenceCount.HasValue
                ? EventRecurrence.GetDates(series, startDate, DateOnly.MaxValue).Count()
                : null,
            Repeat = series.Frequency switch
            {
                RecurrenceFrequency.Weekly => RepeatOption.Weekly,
                RecurrenceFrequency.MonthlyByWeekday when series.WeekOfMonth == EventSeries.LastWeekOfMonth
                    => RepeatOption.MonthlyByLastWeekday,
                RecurrenceFrequency.MonthlyByWeekday => RepeatOption.MonthlyByWeekday,
                RecurrenceFrequency.MonthlyByDate => RepeatOption.MonthlyByDate,
                _ => RepeatOption.SpecificDates
            }
        };

        if (model.Repeat == RepeatOption.SpecificDates)
        {
            model.SpecificDates = upcomingDates
                .Where(d => d != startDate)
                .Select(d => d.ToDateTime(TimeOnly.MinValue))
                .ToList();
            return model;
        }

        // Pattern dates up to the last existing date that are missing were deleted by an admin
        if (upcomingDates.Count > 0)
        {
            DateOnly lastExisting = upcomingDates.Max();
            model.ExcludedDates = model.GetAllDates(startDate, today)
                .Where(d => d <= lastExisting && !upcomingDates.Contains(d))
                .Select(d => d.ToDateTime(TimeOnly.MinValue))
                .ToList();
        }

        return model;
    }
}

/// <summary>
/// One date shown in the preview of a series
/// </summary>
/// <param name="Date">The date, formatted yyyy-MM-dd</param>
/// <param name="Label">The date as shown to the admin</param>
/// <param name="Month">The month heading the date is grouped under</param>
/// <param name="Excluded">True if the date is unchecked and will not be created</param>
public record RecurrencePreviewDate(string Date, string Label, string Month, bool Excluded);

/// <summary>
/// The preview of a series' dates shown while creating or editing it
/// </summary>
/// <param name="Summary">Plain English description of how often the event repeats</param>
/// <param name="Dates">The dates of the series</param>
/// <param name="Errors">Problems with the repeat settings, if any</param>
public record RecurrencePreview(string? Summary, IReadOnlyList<RecurrencePreviewDate> Dates, IReadOnlyList<string> Errors);

/// <summary>
/// Form for changing every upcoming date of a series
/// </summary>
public class EditSeriesViewModel : IValidatableObject
{
    public int SeriesId { get; set; }

    /// <summary>
    /// Time each event starts
    /// </summary>
    [Display(Name = "Starting time")]
    [Required]
    public string StartingTime { get; set; } = null!;

    /// <summary>
    /// Time each event ends
    /// </summary>
    [Display(Name = "Ending time")]
    [Required]
    public string EndingTime { get; set; } = null!;

    /// <summary>
    /// Description of each event
    /// </summary>
    [Required]
    public string Description { get; set; } = null!;

    [Display(Name = "PC2 event")]
    public bool IsPc2Event { get; set; }

    [Display(Name = "County event")]
    public bool IsCountyEvent { get; set; }

    /// <summary>
    /// True to change the dates of the series. Otherwise only the details of upcoming dates are changed
    /// </summary>
    [Display(Name = "Change the dates of this series")]
    public bool ChangeSchedule { get; set; }

    /// <summary>
    /// First date of the changed schedule
    /// </summary>
    [Display(Name = "Starting on")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    public RecurrenceInputModel Recurrence { get; set; } = new();

    /// <summary>
    /// Plain English description of the current schedule, shown on the form
    /// </summary>
    public string? CurrentSchedule { get; set; }

    /// <summary>
    /// Number of upcoming dates in the series, shown on the form
    /// </summary>
    public int UpcomingCount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (ValidationResult result in CalendarEventValidation.ValidateDetails(
            IsPc2Event, IsCountyEvent, StartingTime, EndingTime, nameof(IsCountyEvent), nameof(StartingTime), nameof(EndingTime)))
        {
            yield return result;
        }

        if (!ChangeSchedule)
        {
            yield break;
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly startDate = DateOnly.FromDateTime(StartDate);

        if (startDate < today)
        {
            yield return new ValidationResult("Starting date must be today or in the future", new[] { nameof(StartDate) });
            yield break;
        }

        if (Recurrence.Repeat == RepeatOption.None)
        {
            yield return new ValidationResult(
                "Choose how the series repeats. To keep a single date, delete the series and create a one time event",
                new[] { $"{nameof(Recurrence)}.{nameof(Recurrence.Repeat)}" });
            yield break;
        }

        foreach (ValidationResult result in Recurrence.Validate(startDate, today, nameof(Recurrence)))
        {
            yield return result;
        }
    }
}

/// <summary>
/// Confirmation page for deleting a series
/// </summary>
public class DeleteSeriesViewModel
{
    public int SeriesId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Schedule { get; set; } = string.Empty;

    public TimeOnly StartingTime { get; set; }

    public TimeOnly EndingTime { get; set; }

    public List<DateOnly> UpcomingDates { get; set; } = new();
}

/// <summary>
/// Validation shared by the event forms
/// </summary>
public static class CalendarEventValidation
{
    /// <summary>
    /// Checks that exactly one event type is chosen and the start time is before the end time
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateDetails(bool isPc2Event, bool isCountyEvent,
        string? startingTime, string? endingTime, string eventTypeField, string startingTimeField, string endingTimeField)
    {
        // At least one, but not both, event type must be selected
        if (!isCountyEvent && !isPc2Event)
        {
            yield return new ValidationResult(
                "Please check the PC2 or County Event checkbox",
                new[] { eventTypeField });
        }
        else if (isCountyEvent && isPc2Event)
        {
            yield return new ValidationResult(
                "Please select only one checkbox",
                new[] { eventTypeField });
        }

        // Start time must be before end time
        if (TimeOnly.TryParse(startingTime, out var start) &&
            TimeOnly.TryParse(endingTime, out var end) &&
            start >= end)
        {
            yield return new ValidationResult(
                "Starting time must be before ending time",
                new[] { startingTimeField });

            yield return new ValidationResult(
                "Ending time must be after starting time",
                new[] { endingTimeField });
        }
    }
}
