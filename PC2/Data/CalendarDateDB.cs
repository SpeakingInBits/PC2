using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class CalendarDateDB
    {
        /// <summary>
        /// Gets all current dates and excludes past dates
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task<List<CalendarDate>> GetAllDates(ApplicationDbContext context)
        {
             List<CalendarDate> calendarDate = await (from c in context.CalendarDates
                          where c.Date >= DateTime.Today
                          orderby c.Date
                          select c).Include(nameof(CalendarDate.Events)).ToListAsync();

            // Sorting events based on starting time
            for (int i = 0; i < calendarDate.Count; i++)
            {
                calendarDate[i].Events = calendarDate[i].Events
                    .OrderBy(e => e.StartingTime.Substring(e.StartingTime.IndexOf(" ")))
                    .ThenBy(e => e.StartingTime.Substring(0, e.StartingTime.IndexOf(":")))
                    .ThenBy(e => e.StartingTime.Substring(e.StartingTime.IndexOf(":"))).ToList();
            }
            return calendarDate;
        }

        /// <summary>
        /// Gets a specific date
        /// </summary>
        /// <param name="context"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static async Task<CalendarDate?> GetCalendarDate(ApplicationDbContext context, DateTime date)
        {
            return await (from c in context.CalendarDates
                          where c.Date == date
                          select c).Include(nameof(CalendarDate.Events)).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Attaches an event to a specific date
        /// </summary>
        /// <param name="context"></param>
        /// <param name="date"></param>
        public static void AddCalendarEventToDate(ApplicationDbContext context, CalendarDate date)
        {
            context.CalendarEvents.Attach(date.Events[date.Events.Count - 1]);
        }

        /// <summary>
        /// Adds a date to the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static async Task AddCalendarDate(ApplicationDbContext context, CalendarDate date)
        {
            context.CalendarDates.Add(date);
            await context.SaveChangesAsync();
        }
    }
}
