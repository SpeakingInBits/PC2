using System.ComponentModel.DataAnnotations;

namespace PC2.Models;

public class CalendarEvent : IComparable<CalendarEvent>
{
    [Key]
    public int CalendarEventID { get; set; }

    /// <summary>
    /// Day the event is taking place
    /// </summary>
    [Display(Name = "Date of event")]
    public DateOnly DateOfEvent { get; set; }

    /// <summary>
    /// Start time of the event
    /// </summary>
    [Display(Name = "Starting time")]
    [Required]
    public TimeOnly StartingTime {  get; set; }

    /// <summary>
    /// End time of the event
    /// </summary>
    [Display(Name = "Ending time")]
    [Required]
    public TimeOnly EndingTime {  get; set; }

    /// <summary>
    /// Description of the Event
    /// </summary>
    [Display(Name = "Description")]
    [Required]
    public string EventDescription { get; set; } = null!;

    /// <summary>
    /// True if the event is a PC2 event
    /// </summary>
    [Display(Name = "PC2 event")]
    public bool PC2Event {  get; set; }

    /// <summary>
    /// True if the event is a county event
    /// </summary>
    [Display(Name = "County event")]
    public bool CountyEvent {  get; set; }

    /// <summary>
    /// The series this event is part of, or null for a one time event
    /// </summary>
    public int? EventSeriesID { get; set; }

    public EventSeries? EventSeries { get; set; }

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
    [Display(Name = "Date of event")]
    [DataType(DataType.Date)]
    public DateTime DateOfEvent { get; set; }

    /// <summary>
    /// Time the event starts
    /// </summary>
    [Display(Name = "Starting time")]
    [Required]
    public string StartingTime { get; set; } = null!;

    /// <summary>
    /// Time the event ends
    /// </summary>
    [Display(Name = "Ending time")]
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
    [Display(Name = "PC2 event")]
    public bool IsPc2Event { get; set; }

    /// <summary>
    /// Is the event a county sponsored event
    /// </summary>
    [Display(Name = "County event")]
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
                new[] { nameof(IsCountyEvent) });
        }
        else if (IsCountyEvent && IsPc2Event)
        {
            yield return new ValidationResult(
                "Please select only one checkbox",
                new[] { nameof(IsCountyEvent) });
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