namespace PC2.Models
{
    public class EventsModel
    {
        public List<CalendarEvent> CalendarEvents {  get; set; }

        public bool IsPC2EventCalendar { get; set; } = true;
    }
}
