using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class CalendarDateDB
    {
        public static async Task<List<CalendarDate>> GetAllDates(ApplicationDbContext context)
        {
            return await (from c in context.CalendarDates
                          select c).Include(nameof(CalendarDate.Events)).ToListAsync();
        }

        public static async Task<CalendarDate?> GetCalendarDate(ApplicationDbContext context, DateTime date)
        {
            return await (from c in context.CalendarDates
                          where c.Date == date
                          select c).Include(nameof(CalendarDate.Events)).FirstOrDefaultAsync();
        }

        public static void AddCalendarEventToDate(ApplicationDbContext context, CalendarDate date)
        {
            context.CalendarEvents.Attach(date.Events[date.Events.Count - 1]);
        }

        public static async Task AddCalendarDate(ApplicationDbContext context, CalendarDate date)
        {
            context.CalendarDates.Add(date);
            await context.SaveChangesAsync();
        }
    }
}
