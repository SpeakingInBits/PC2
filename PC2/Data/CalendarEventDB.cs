using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class CalendarEventDB
    {
        /// <summary>
        /// PC2 is in Tacoma, so event dates are Pacific time dates. The server's clock is UTC on Azure.
        /// </summary>
        private static readonly TimeZoneInfo PacificTime = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

        /// <summary>
        /// Today's date in Pacific time. Worked out on each call, so events drop off the day after they happen.
        /// </summary>
        /// <param name="timeProvider">The clock to use; the system clock when null</param>
        public static DateOnly Today(TimeProvider? timeProvider = null)
        {
            DateTimeOffset now = (timeProvider ?? TimeProvider.System).GetUtcNow();
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, PacificTime).DateTime);
        }

        /// <summary>
        /// Retrieve all events for the current day and future dates
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task<List<CalendarEvent>> GetAllEvents(ApplicationDbContext context)
        {
            return await GetUpcomingEventsAsync(context, null, null);
        }

        /// <summary>
        /// Gets the events from today onwards, optionally limited to a date range
        /// </summary>
        /// <param name="start">The first date to include, or null for no lower limit other than today</param>
        /// <param name="end">The date to stop before (exclusive), or null for no upper limit</param>
        public static async Task<List<CalendarEvent>> GetUpcomingEventsAsync(ApplicationDbContext context, DateOnly? start, DateOnly? end)
        {
            DateOnly today = Today();
            DateOnly from = start.HasValue && start.Value > today ? start.Value : today;

            IQueryable<CalendarEvent> events = context.CalendarEvents.AsNoTracking()
                .Where(e => e.DateOfEvent >= from);
            if (end.HasValue)
            {
                events = events.Where(e => e.DateOfEvent < end.Value);
            }

            return await events
                .OrderBy(e => e.DateOfEvent)
                .ThenBy(e => e.StartingTime)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all past events.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>A list of all past events</returns>
        public static async Task<List<CalendarEvent>> GetAllPastEvents(ApplicationDbContext context)
        {
            DateOnly today = Today();
            return await (from calEvents in context.CalendarEvents
                          where calEvents.DateOfEvent < today
                          select calEvents).ToListAsync();
        }

        /// <summary>
        /// Adds an event to the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="calendarEvent"></param>
        /// <returns></returns>
        public static async Task AddEvent(ApplicationDbContext context, CalendarEvent calendarEvent)
        {
            context.CalendarEvents.Add(calendarEvent);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets an event based on ID
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<CalendarEvent?> GetEvent(ApplicationDbContext context, int id)
        {
            return await (from c in context.CalendarEvents
                          where c.CalendarEventID == id
                          select c).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Update a CalendarEvent entry
        /// </summary>
        /// <param name="context"></param>
        /// <param name="calendarEvent"></param>
        /// <returns>True if calendar was updated, false if not</returns>
        public static async Task<bool> UpdateEvent(ApplicationDbContext context, CalendarEvent calendarEvent)
        {
            try
            {
                context.Entry(calendarEvent).State = EntityState.Modified;
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Delete target event and remove parent date from Calendar if no entries remain from same day
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task DeleteEvent(ApplicationDbContext context, int id)
        {
            CalendarEvent? targetEvent = await context.CalendarEvents.FindAsync(id);

            if (targetEvent == null)
                return;

            context.Entry(targetEvent).State = EntityState.Deleted;
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Delete all past events from the database
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task DeletePastEvents(ApplicationDbContext context)
        {
            List<CalendarEvent> pastEvents = await GetAllPastEvents(context);
            if (pastEvents.Count == 0)
                return;

            // One save deletes them all
            context.CalendarEvents.RemoveRange(pastEvents);
            await context.SaveChangesAsync();
        }
    }
}
