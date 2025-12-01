namespace PC2.Models
{
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
}
