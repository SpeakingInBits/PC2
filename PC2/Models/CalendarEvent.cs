using System.ComponentModel.DataAnnotations;

namespace PC2.Models;

public class CalendarEvent : IComparable<CalendarEvent>
{
    [Key]
    public int CalendarEventID { get; set; }

    /// <summary>
    /// Day the event is taking place
    /// </summary>
    public DateOnly DateOfEvent { get; set; }

    /// <summary>
    /// Start time of the event
    /// </summary>
    [Required]
    public TimeOnly StartingTime {  get; set; }

    /// <summary>
    /// End time of the event
    /// </summary>
    [Required]
    public TimeOnly EndingTime {  get; set; }

    /// <summary>
    /// Description of the Event
    /// </summary>
    [Required]
    public string EventDescription { get; set; } = null!;

    /// <summary>
    /// True if the event is a PC2 event
    /// </summary>
    public bool PC2Event {  get; set; }

    /// <summary>
    /// True if the event is a county event
    /// </summary>
    public bool CountyEvent {  get; set; }

    // Convert DateOnly and TimeOnly to DateTime
    public DateTime StartingDateTime
    {
        get
        {
            return DateOfEvent.ToDateTime(StartingTime);
        }
    }

    public DateTime EndingDateTime
    {
        get
        {
            return DateOfEvent.ToDateTime(EndingTime);
        }
    }

    public int CompareTo(CalendarEvent? other)
    {
        return this.DateOfEvent.CompareTo(other.DateOfEvent);
    }
}

public class CalendarCreateEventViewModel : IValidatableObject
{
    /// <summary>
    /// PK value used to Edit/Delete event
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The date of the event
    /// </summary>
    [DataType(DataType.Date)]
    public DateTime DateOfEvent { get; set; }

    /// <summary>
    /// Time the event starts
    /// </summary>
    [Required]
    public string StartingTime { get; set; } = null!;

    /// <summary>
    /// Time the event ends
    /// </summary>
    [Required]
    public string EndingTime { get; set; } = null!;

    /// <summary>
    /// Description of the event
    /// </summary>
    [Required]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Is the event a PC2 sponsored event
    /// </summary>
    public bool IsPc2Event { get; set; }

    /// <summary>
    /// Is the event a county sponsored event
    /// </summary>
    public bool IsCountyEvent { get; set; }

    /// <summary>
    /// Validates the current object based on a set of predefined rules.
    /// </summary>
    /// <param name="validationContext">The context in which the validation is performed. This parameter provides additional information  about the
    /// object being validated.</param>
    /// <returns>An <see cref="IEnumerable{ValidationResult}"/> containing the validation errors, if any.  If the object is
    /// valid, the collection will be empty.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // At least one, but not both, event type must be selected
        if (!IsCountyEvent && !IsPc2Event)
        {
            yield return new ValidationResult(
                "Please check the PC2 or County Event checkbox",
                new[] { nameof(IsCountyEvent), nameof(IsPc2Event) });
        }
        if (IsCountyEvent && IsPc2Event)
        {
            yield return new ValidationResult(
                "Please select only one checkbox",
                new[] { nameof(IsCountyEvent), nameof(IsPc2Event) });
        }

        // Date must be today or in the future
        if (DateOfEvent.Date < DateTime.Now.Date)
        {
            yield return new ValidationResult(
                "Starting day must be at a current or future date",
                new[] { nameof(DateOfEvent) });
        }

        // Start time must be before end time
        if (TimeOnly.TryParse(StartingTime, out var start) &&
            TimeOnly.TryParse(EndingTime, out var end))
        {
            if (start >= end)
            {
                yield return new ValidationResult(
                    "Starting time must be before ending time",
                    new[] { nameof(StartingTime) });

                yield return new ValidationResult(
                    "Ending time must be after starting time",
                    new[] { nameof(EndingTime) });
            }
        }
    }
}

/// <summary>
/// View model for displaying calendar events with sanitized HTML descriptions.
/// </summary>
public class CalendarEventViewModel
{
    /// <summary>
    /// The original calendar event data
    /// </summary>
    public CalendarEvent Event { get; set; } = null!;

    /// <summary>
    /// HTML-encoded event description with clickable links for URLs, emails, and phone numbers.
    /// Safe to render using @Html.Raw()
    /// </summary>
    public string SanitizedDescription { get; set; } = string.Empty;
}