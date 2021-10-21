using PC2.Models;

namespace PC2.Data
{
    public static class CalendarEventDB
    {
        public static async Task AddEvent(ApplicationDbContext context, CalendarEvent calendarEvent)
        {
            context.CalendarEvents.Add(calendarEvent);
            context.CalendarDates.Attach(calendarEvent.CalendarDate);
            await context.SaveChangesAsync();
        }
    }
}
