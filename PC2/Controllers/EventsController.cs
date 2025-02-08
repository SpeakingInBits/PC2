using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index(string? eventType)
        {
            bool getPc2Events = eventType == null;
            EventsModel eventsModel = new()
            {
                IsPC2EventCalendar = getPc2Events, 
                CalendarEvents = await CalendarEventDB.GetAllEvents(_context, getPc2Events)
            };

            // Load events based on the eventType
            if (eventType == "Meeting")
            {
                return RedirectToAction("StandingMeeting");
            }

            return View(eventsModel);
        }

        public async Task<IActionResult> StandingMeeting()
        {
            var meetings = await _context.CalendarEvents
                             .Where(e => e.StandingMeeting)
                             .ToListAsync();

            return View(meetings);
        }

        // Returns events in JSON format for FullCalendar
        public async Task<IActionResult> GetEvents(string? eventType)
        {
            bool getPc2Events = string.IsNullOrEmpty(eventType) || eventType == "PC2";

            // Get events from the database
            List<CalendarEvent> events = await CalendarEventDB.GetAllEvents(_context, getPc2Events);

            // Create a list to hold FullCalendar-compatible events
            List<object> fullCalendarEvents = new List<object>();

            // Loop through the events and add them to the list
            foreach (var e in events)
            {
                var calendarEvent = new
                {
                    title = e.EventDescription,
                    start = e.StartingDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),  // FullCalendar format
                    end = e.EndingDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),      // FullCalendar format
                };
                fullCalendarEvents.Add(calendarEvent);
            }

            return Json(fullCalendarEvents);
        }
    }
}
