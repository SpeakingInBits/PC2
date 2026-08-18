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
        /// Event descriptions are HTML-encoded and links, emails, and phone numbers are converted to clickable HTML links.
        /// </summary>
        /// <remarks>This method is intended for use by pages that require event data
        /// formatted for the FullCalendar JavaScript library. The returned list will be empty if no events are
        /// found.</remarks>
        /// <returns>A JSON result containing a list of event objects, where each object includes the sanitized event title with clickable links,
        /// start and end date-times in ISO 8601 format, and a flag indicating whether the event is a PC2 event.</returns>
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
        

        public IActionResult Create()
        {
            return View(new CalendarCreateEventViewModel());
        }


        [HttpPost]
        public async Task<IActionResult> Create(CalendarCreateEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Build RRULE from UI fields
            string? rrule = BuildRRule(
                model.Frequency,
                model.Interval,
                model.EndType,
                model.Count,
                model.Until,
                model.Ordinal,
                model.Weekday
            );

            // Convert to your CalendarEvent model
            CalendarEvent evt = new CalendarEvent
            {
                DateOfEvent = DateOnly.FromDateTime(model.DateOfEvent),
                StartingTime = TimeOnly.Parse(model.StartingTime),
                EndingTime = TimeOnly.Parse(model.EndingTime),
                EventDescription = model.Description,
                PC2Event = model.IsPc2Event,
                CountyEvent = model.IsCountyEvent,

                // NEW — store recurrence rule
                RRule = rrule
            };

            _context.CalendarEvents.Add(evt);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private string? BuildRRule
        (
        string? freq,
        int? interval,
        string? endType,
        int? count,
        DateTime? until,
        int? ordinal,
        string? weekday
        )
        {
            if (string.IsNullOrWhiteSpace(freq))
                return null;

            var parts = new List<string> { $"FREQ={freq}" };

            if (interval.HasValue && interval.Value > 1)
                parts.Add($"INTERVAL={interval.Value}");

            if (endType == "COUNT" && count.HasValue)
                parts.Add($"COUNT={count.Value}");
            else if (endType == "UNTIL" && until.HasValue)
                parts.Add($"UNTIL={until.Value.ToUniversalTime():yyyyMMdd'T'HHmmss'Z'}");

            if (!string.IsNullOrWhiteSpace(weekday))
            {
                if (ordinal.HasValue)
                    parts.Add($"BYDAY={ordinal}{weekday}");
                else
                    parts.Add($"BYDAY={weekday}");
            }

            return string.Join(";", parts);
        }
    }
}
