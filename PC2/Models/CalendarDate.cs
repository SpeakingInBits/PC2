using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class CalendarDate
    {
        [Key]
        public int CalendarDateID {  get; set; }

        [Required]
        public int DayOfMonth {  get; set; }

        [Required]
        public string DayOfWeek {  get; set; }

        [Required]
        public string Month {  get; set; }

        [Required]
        public string Year {  get; set; }

        public List<CalendarEvent> CalendarEvents {  get; set; } = new List<CalendarEvent>();
    }
}
