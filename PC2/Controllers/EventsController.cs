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

        // Returns events in JSON format for FullCalendar
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
                    title = e.EventDescription,
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
