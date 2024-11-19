using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
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

            return View(eventsModel);
        }

        // Returns events in JSON format for FullCalendar
        public async Task<IActionResult> GetEvents(string? eventType)
        {
            bool getPc2Events = eventType == null;

            // Get events from the database
            List<CalendarEvent> events = await CalendarEventDB.GetAllEvents(_context, getPc2Events);

            // Holds simplified calendar events for FullCalendar
            List<CalendarEvent> fullCalendarEvents = new List<CalendarEvent>();

            // Loop through the events and add them to the list
            foreach (CalendarEvent e in events)
            {
                var calendarEvent = new CalendarEvent
                {
                    EventDescription = e.EventDescription,
                    DateOfEvent = e.DateOfEvent,
                    StartingTime = e.StartingTime,
                    EndingTime = e.EndingTime
                };

                fullCalendarEvents.Add(calendarEvent);
            }

            // Return events as JSON
            return Json(fullCalendarEvents);
        }
    }
}
