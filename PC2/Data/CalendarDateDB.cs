using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class CalendarDateDB
    {
        public static async Task<List<CalendarDate>> GetAllDates(ApplicationDbContext context)
        {
            return await (from c in context.CalendarDates
                          select c).Include(nameof(CalendarDate.CalendarEvents)).ToListAsync();
        }
    }
}
