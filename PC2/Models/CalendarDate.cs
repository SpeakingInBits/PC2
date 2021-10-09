using System.ComponentModel.DataAnnotations;

namespace PC2.Models
{
    public class CalendarDate
    {
        [Key]
        public int CalendarDateID {  get; set; }

        public DateTime Date {  get; set; }

        public List<CalendarEvent> Events {  get; set; }
    }
}
