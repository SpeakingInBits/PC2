using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
                          select c).Include(nameof(CalendarEvent.CalendarDate)).FirstOrDefaultAsync();
        }

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
    }
}
