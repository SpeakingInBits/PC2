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
                IEnumerable<CalendarEvent> occurrences = ExpandRecurrence(e);

                foreach (var occ in occurrences)
                {
                    var calendarEvent = new
                    {
                        title = TextLinkifier.Linkify(occ.EventDescription),
                        start = occ.StartingDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = occ.EndingDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        isPC2Event = occ.PC2Event,
                        eventId = e.CalendarEventID,
                    };

                    fullCalendarEvents.Add(calendarEvent);
                }
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

        // =======================
        // EDIT (GET)
        // =======================
        public async Task<IActionResult> Edit(int id)
        {
            CalendarEvent? evt = await _context.CalendarEvents.FindAsync(id);

            if (evt == null)
                return NotFound();

            // Parse RRULE into ViewModel fields
            CalendarCreateEventViewModel model = new CalendarCreateEventViewModel
            {
                EventId = evt.CalendarEventID,
                DateOfEvent = evt.DateOfEvent.ToDateTime(TimeOnly.MinValue),
                StartingTime = evt.StartingTime.ToString("HH:mm"),
                EndingTime = evt.EndingTime.ToString("HH:mm"),
                Description = evt.EventDescription,
                IsPc2Event = evt.PC2Event,
                IsCountyEvent = evt.CountyEvent
            };

            if (!string.IsNullOrWhiteSpace(evt.RRule))
            {
                var parts = evt.RRule.Split(';');

                foreach (var part in parts)
                {
                    if (part.StartsWith("FREQ="))
                        model.Frequency = part.Replace("FREQ=", "");

                    if (part.StartsWith("INTERVAL="))
                        model.Interval = int.Parse(part.Replace("INTERVAL=", ""));

                    if (part.StartsWith("COUNT="))
                    {
                        model.EndType = "COUNT";
                        model.Count = int.Parse(part.Replace("COUNT=", ""));
                    }

                    if (part.StartsWith("UNTIL="))
                    {
                        model.EndType = "UNTIL";
                        string untilStr = part.Replace("UNTIL=", "");
                        model.Until = DateTime.ParseExact(untilStr, "yyyyMMdd'T'HHmmss'Z'", null);
                    }

                    if (part.StartsWith("BYDAY="))
                    {
                        string value = part.Replace("BYDAY=", "");

                        // Ordinal + weekday (e.g., 2MO)
                        if (value.Length > 2)
                        {
                            model.Ordinal = int.Parse(value.Substring(0, value.Length - 2));
                            model.Weekday = value.Substring(value.Length - 2);
                        }
                        else
                        {
                            model.Weekday = value;
                        }
                    }
                }

                if (model.EndType == null)
                    model.EndType = "NEVER";
            }

            return View(model);
        }

        // =======================
        // EDIT (POST)
        // =======================
        [HttpPost]
        public async Task<IActionResult> Edit(CalendarCreateEventViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            CalendarEvent? evt = await _context.CalendarEvents.FindAsync(model.EventId);

            if (evt == null)
                return NotFound();

            // Build updated RRULE
            string? rrule = BuildRRule(
                model.Frequency,
                model.Interval,
                model.EndType,
                model.Count,
                model.Until,
                model.Ordinal,
                model.Weekday
            );

            // Update event
            evt.DateOfEvent = DateOnly.FromDateTime(model.DateOfEvent);
            evt.StartingTime = TimeOnly.Parse(model.StartingTime);
            evt.EndingTime = TimeOnly.Parse(model.EndingTime);
            evt.EventDescription = model.Description;
            evt.PC2Event = model.IsPc2Event;
            evt.CountyEvent = model.IsCountyEvent;
            evt.RRule = rrule;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private IEnumerable<CalendarEvent> ExpandRecurrence(CalendarEvent evt)
        {
            List<CalendarEvent> expanded = new();

            if (string.IsNullOrWhiteSpace(evt.RRule))
            {
                expanded.Add(evt);
                return expanded;
            }

            // Parse RRULE
            var parts = evt.RRule.Split(';')
                .Select(p => p.Split('='))
                .ToDictionary(p => p[0], p => p[1]);

            string freq = parts.ContainsKey("FREQ") ? parts["FREQ"] : "";
            int interval = parts.ContainsKey("INTERVAL") ? int.Parse(parts["INTERVAL"]) : 1;

            int? count = parts.ContainsKey("COUNT") ? int.Parse(parts["COUNT"]) : null;
            DateTime? until = parts.ContainsKey("UNTIL")
                ? DateTime.ParseExact(parts["UNTIL"], "yyyyMMdd'T'HHmmss'Z'", null)
                : null;

            string? byday = parts.ContainsKey("BYDAY") ? parts["BYDAY"] : null;

            // Start at the original event date
            DateTime current = evt.StartingDateTime;
            int occurrences = 0;

            // Expand up to 1 year ahead (or until COUNT/UNTIL)
            DateTime limit = DateTime.Now.AddYears(1);

            while (true)
            {
                // Stop if COUNT reached
                if (count.HasValue && occurrences >= count.Value)
                    break;

                // Stop if UNTIL reached
                if (until.HasValue && current > until.Value)
                    break;

                // Stop if beyond 1-year limit
                if (current > limit)
                    break;

                // Add occurrence
                expanded.Add(new CalendarEvent
                {
                    CalendarEventID = evt.CalendarEventID,
                    DateOfEvent = DateOnly.FromDateTime(current),
                    StartingTime = evt.StartingTime,
                    EndingTime = evt.EndingTime,
                    EventDescription = evt.EventDescription,
                    PC2Event = evt.PC2Event,
                    CountyEvent = evt.CountyEvent,
                    RRule = evt.RRule
                });

                occurrences++;

                // Advance based on frequency
                switch (freq)
                {
                    case "DAILY":
                        current = current.AddDays(interval);
                        break;

                    case "WEEKLY":
                        current = current.AddDays(7 * interval);
                        break;

                    case "MONTHLY":
                        current = current.AddMonths(interval);
                        break;

                    case "YEARLY":
                        current = current.AddYears(interval);
                        break;

                    default:
                        return expanded;
                }
            }

            return expanded;
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
