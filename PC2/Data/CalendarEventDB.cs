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

            CalendarDate? parentDate = await context.CalendarDates.Where(c => c.Events.Count == 1).FirstOrDefaultAsync();

            // Only a single event was found this day, delete entire date from Calendar
            if (parentDate != null)
            {
                context.Entry(parentDate).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }
            else // More than one event was found this day, delete target event only
            {
                context.Entry(targetEvent).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }
        }
    }
}
