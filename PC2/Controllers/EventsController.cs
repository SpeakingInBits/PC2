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

        public async Task<IActionResult> Index(string? eventType)
        {
            List<CalendarEvent> calendarEvents = await CalendarEventDB.GetAllEvents(_context);

            // delete all events that are before the current date
            foreach (CalendarEvent calendarEvent in calendarEvents)
            {
                // convert today to a DateOnly object
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                
                if (calendarEvent.DateOfEvent < today)
                {
                    await CalendarEventDB.DeleteEvent(_context, calendarEvent.CalendarEventID);
                    calendarEvents = await CalendarEventDB.GetAllEvents(_context);
                }
            }

            bool getPc2Events = eventType == null;
            EventsModel eventsModel = new()
            {
                IsPC2EventCalendar = getPc2Events,
                CalendarEvents = await CalendarEventDB.GetAllEvents(_context, getPc2Events)
            };

            return View(eventsModel);
        }
    }
}
