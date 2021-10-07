using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class CalendarEvent
    {
        [Key]
        public int CalendarEventID { get; set; }

        [Required]
        public string StartingTime {  get; set; }

        [Required]
        public string EndingTime {  get; set; }

        [Required]
        public string EventDescription {  get; set; }

        public List<CalendarDate> CalendarDate {  get; set; } = new List<CalendarDate>();
    }
}
