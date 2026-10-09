using Microsoft.EntityFrameworkCore;
using PC2.Models;

namespace PC2.Data
{
    public static class EventSeriesDB
    {
        /// <summary>
        /// Adds a series to the database along with an event for each date
        /// </summary>
        /// <param name="context"></param>
        /// <param name="series">The series to add</param>
        /// <param name="dates">The dates to create events on</param>
        public static async Task AddSeries(ApplicationDbContext context, EventSeries series, IEnumerable<DateOnly> dates)
        {
            foreach (DateOnly date in dates.Distinct().Order())
            {
                series.Events.Add(EventRecurrence.CreateOccurrence(series, date));
            }

            context.EventSeries.Add(series);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a series and its events, ordered by date
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <returns>The series, or null if it does not exist</returns>
        public static async Task<EventSeries?> GetSeries(ApplicationDbContext context, int id)
        {
            return await context.EventSeries
                .Include(s => s.Events.OrderBy(e => e.DateOfEvent))
                .FirstOrDefaultAsync(s => s.EventSeriesID == id);
        }

        /// <summary>
        /// Deletes a series and all of its events
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        public static async Task DeleteSeries(ApplicationDbContext context, int id)
        {
            EventSeries? series = await GetSeries(context, id);

            if (series == null)
                return;

            context.EventSeries.Remove(series);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates events for series with no end, so each one has events
        /// <see cref="EventRecurrence.OpenEndedHorizonMonths"/> months ahead of <paramref name="today"/>.
        /// Only dates after <see cref="EventSeries.GeneratedThrough"/> are added, so dates that were deleted stay deleted
        /// </summary>
        /// <param name="context"></param>
        /// <param name="today">The current date</param>
        public static async Task ExtendOpenEndedSeries(ApplicationDbContext context, DateOnly today)
        {
            DateOnly horizon = today.AddMonths(EventRecurrence.OpenEndedHorizonMonths);

            List<EventSeries> seriesToExtend = await context.EventSeries
                .Where(s => s.Frequency != RecurrenceFrequency.SpecificDates
                         && s.EndDate == null
                         && s.OccurrenceCount == null
                         && (s.GeneratedThrough == null || s.GeneratedThrough < horizon))
                .ToListAsync();

            if (seriesToExtend.Count == 0)
                return;

            foreach (EventSeries series in seriesToExtend)
            {
                DateOnly from = series.GeneratedThrough?.AddDays(1) ?? series.StartDate;

                foreach (DateOnly date in EventRecurrence.GetDates(series, from, horizon))
                {
                    context.CalendarEvents.Add(EventRecurrence.CreateOccurrence(series, date));
                }

                series.GeneratedThrough = horizon;
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes series that have no events left and will not have any more created
        /// </summary>
        /// <param name="context"></param>
        public static async Task DeleteFinishedSeries(ApplicationDbContext context)
        {
            List<EventSeries> finishedSeries = await context.EventSeries
                .Where(s => !s.Events.Any()
                         && (s.Frequency == RecurrenceFrequency.SpecificDates
                             || s.EndDate != null
                             || s.OccurrenceCount != null))
                .ToListAsync();

            if (finishedSeries.Count == 0)
                return;

            context.EventSeries.RemoveRange(finishedSeries);
            await context.SaveChangesAsync();
        }
    }
}
