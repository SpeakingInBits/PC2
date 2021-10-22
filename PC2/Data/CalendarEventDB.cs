using PC2.Models;

namespace PC2.Data
{
    public static class CalendarEventDB
    {
        /// <summary>
        /// Adds an event to the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="calendarEvent"></param>
        /// <returns></returns>
        public static async Task AddEvent(ApplicationDbContext context, CalendarEvent calendarEvent)
        {
            context.CalendarEvents.Add(calendarEvent);
            context.CalendarDates.Attach(calendarEvent.CalendarDate);
            await context.SaveChangesAsync();
        }
    }
}
