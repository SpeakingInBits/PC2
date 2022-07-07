namespace PC2.Models
{
    public class EventsModel
    {
        public List<CalendarDate> CalendarDate {  get; set; }

        public bool IsPC2EventCalendar { get; set; } = true;
    }
}
