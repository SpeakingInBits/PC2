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
                CalendarEvents = await CalendarEventDB.GetAllEvents(_context, getPc2Events),
                StandingMeetings = await GetStandingMeetings()
            };

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
            // Get all PC2 and County events from the database, excluding standing meetings
            List<CalendarEvent> events = await _context.CalendarEvents
                .Where(e => !e.StandingMeeting && (e.PC2Event || e.CountyEvent))
                .ToListAsync();

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
                    isPC2Event = e.PC2Event // Include the isPC2Event property
                };
                fullCalendarEvents.Add(calendarEvent);
            }

            return Json(fullCalendarEvents);
        }
        /// <summary>
        /// This method to get all the standing meeting events
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetStandingMeetings()
        {
            return await _context.CalendarEvents
                .Where(e => e.StandingMeeting)
                .Select(e => e.EventDescription + " - " + e.StartingDateTime.ToString("dddd h:mm tt"))
                .ToListAsync();
        }

    }
}
