using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// The events calendar. It loads its events from <see cref="GetEvents"/>.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Retrieves all calendar events and returns them in a format compatible with FullCalendar.
        /// Event descriptions are HTML-encoded and links, emails, and phone numbers are converted to clickable HTML links.
        /// </summary>
        /// <remarks>This method is intended for use by pages that require event data
        /// formatted for the FullCalendar JavaScript library. The returned list will be empty if no events are
        /// found.</remarks>
        /// <param name="start">The first date the calendar is showing. Past events are never included.</param>
        /// <param name="end">The date after the last one the calendar is showing (exclusive)</param>
        /// <returns>A JSON result containing a list of event objects, where each object includes the sanitized event title with clickable links,
        /// start and end date-times in ISO 8601 format, and a flag indicating whether the event is a PC2 event.</returns>
        public async Task<IActionResult> GetEvents(DateOnly? start = null, DateOnly? end = null)
        {
            // Get the upcoming events in the range the calendar is showing
            List<CalendarEvent> events = await CalendarEventDB.GetUpcomingEventsAsync(_context, start, end);

            // Create a list to hold FullCalendar-compatible events
            List<object> fullCalendarEvents = new List<object>();

            // Loop through the events and add them to the list
            foreach (CalendarEvent e in events)
            {
                var calendarEvent = new
                {
                    title = TextLinkifier.Linkify(e.EventDescription),
                    start = e.StartingDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),  // FullCalendar format
                    end = e.EndingDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),      // FullCalendar format
                    isPC2Event = e.PC2Event,
                };
                fullCalendarEvents.Add(calendarEvent);
            }

            return Json(fullCalendarEvents);
        }
    }
}
