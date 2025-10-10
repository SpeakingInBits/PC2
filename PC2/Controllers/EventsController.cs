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

        public async Task<IActionResult> Index()
        {
            EventsModel eventsModel = new()
            {
                CalendarEvents = await CalendarEventDB.GetAllEvents(_context)
            };

            return View(eventsModel);
        }

        /// <summary>
        /// Retrieves all calendar events and returns them in a format compatible with FullCalendar.
        /// Links, emails, and phone numbers in event descriptions are converted to clickable HTML links.
        /// </summary>
        /// <remarks>This method is intended for use by pages that require event data
        /// formatted for the FullCalendar JavaScript library. The returned list will be empty if no events are
        /// found.</remarks>
        /// <returns>A JSON result containing a list of event objects, where each object includes the event title, start and end
        /// date-times in ISO 8601 format, and a flag indicating whether the event is a PC2 event.</returns>
        public async Task<IActionResult> GetEvents()
        {
            // Get events from the database
            List<CalendarEvent> events = await CalendarEventDB.GetAllEvents(_context);

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
