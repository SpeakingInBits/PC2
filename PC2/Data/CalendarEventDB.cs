using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class CalendarEventDB
    {
        /// <summary>
        /// Get all upcoming events for the calendar
        /// </summary>
        /// <param name="pc2Events">If true, pull PC2 events, otherwise pull county events</param>
        public static async Task<List<CalendarEvent>> GetAllPC2Events(ApplicationDbContext context, bool pc2Events)
        {
            return await (from calEvents in context.CalendarEvents
                          where calEvents.PC2Event == pc2Events
                          orderby calEvents.DateOfEvent descending
                          orderby calEvents.StartingTime ascending
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
    }
}
