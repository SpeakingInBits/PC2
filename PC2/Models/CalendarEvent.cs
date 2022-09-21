using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
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

        public int CompareTo(CalendarEvent? other)
        {
            return this.DateOfEvent.CompareTo(other.DateOfEvent);
        }
    }
}
