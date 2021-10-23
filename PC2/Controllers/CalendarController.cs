using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            EventsModel eventsModel = new EventsModel();
            eventsModel.CalendarDate = await CalendarDateDB.GetAllDates(_context);
            return View(eventsModel);
        }

        /// <summary>
        /// Creates a calendar event and date
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string date, string startingTime, string endingTime, string description)
        {
            CalendarDate calendarDate = new CalendarDate();
            calendarDate.Date = DateTime.Parse(date);

            CalendarEvent calendarEvent = new CalendarEvent();
            startingTime = FormatStartingTime(startingTime);
            endingTime = FormatEndingTime(endingTime);

            calendarEvent.StartingTime = startingTime;
            calendarEvent.EndingTime = endingTime;
            calendarEvent.EventDescription = description;

            if (Request.Form["pc2"] == "PC2")
            {
                calendarEvent.PC2Event = true;
            }
            else
            {
                calendarEvent.CountyEvent = true;
            }

            // Checking to see if the date already exists in the database
            CalendarDate existingDate = await CalendarDateDB.GetCalendarDate(_context, calendarDate.Date);

            if (existingDate != null)
            {
                calendarEvent.CalendarDate = existingDate;
                await CalendarEventDB.AddEvent(_context, calendarEvent);
                existingDate.Events.Add(calendarEvent);
                CalendarDateDB.AddCalendarEventToDate(_context, existingDate);
            }
            else
            {
                await CalendarDateDB.AddCalendarDate(_context, calendarDate);
                calendarEvent.CalendarDate = calendarDate;
                await CalendarEventDB.AddEvent(_context, calendarEvent);
                calendarDate.Events.Add(calendarEvent);
                CalendarDateDB.AddCalendarEventToDate(_context, calendarDate);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edits an event based on event id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            CalendarEvent calendarEvent = await CalendarEventDB.GetEvent(_context, id);
            calendarEvent.StartingTime = UnformatTime(calendarEvent.StartingTime);
            calendarEvent.EndingTime = UnformatTime(calendarEvent.EndingTime);

            return View(calendarEvent);
        }

        /// <summary>
        /// Returns the time to a format that the input can read
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private static string UnformatTime(string time)
        {
            int temp = Int32.Parse(time.Substring(0, time.IndexOf(":")));
            string ampmTemp = time.Substring(time.IndexOf(" ") + 1);
            string backPortion = "";

            // If the time is two digits then indexof goes 1 too far
            // If the time is one digit then indexof goes 2 too far
            if (temp > 9)
            {
                backPortion = time.Substring(time.IndexOf(":"), time.IndexOf(" ") - 2);
            }
            else
            {
                backPortion = time.Substring(time.IndexOf(":"), time.IndexOf(" ") - 1);
            }

            string frontPortion = "";

            if (ampmTemp == "PM" && temp != 13 && temp != 12)
            {
                temp += 12;
                frontPortion = temp + "";
            }
            else if (temp < 10)
            {
                frontPortion = "0" + temp;
            }
            else
            {
                frontPortion = temp + "";
            }

            return frontPortion + backPortion;
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CalendarEvent calendarEvent)
        {
            // If event type is changed the opposite type needs to be set to false
            if (calendarEvent.PC2Event)
            {
                calendarEvent.CountyEvent = false;
            }
            else
            {
                calendarEvent.PC2Event = false;
            }

            calendarEvent.StartingTime = FormatStartingTime(calendarEvent.StartingTime);
            calendarEvent.EndingTime = FormatEndingTime(calendarEvent.EndingTime);

            bool success = await CalendarEventDB.UpdateEvent(_context, calendarEvent);

            if (!success)
            {
                TempData["UpdateFailed"] = "An error occured updating the event";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Formats the ending time from a 24 hour time to a standard 12 hour
        /// with am or pm attached
        /// </summary>
        /// <param name="endingTime"></param>
        /// <returns></returns>
        private static string FormatEndingTime(string endingTime)
        {
            int temp = Int32.Parse(endingTime.Substring(0, 2));
            if (temp > 12)
            {
                temp -= 12;
                endingTime = temp + endingTime.Substring(2) + " PM";
            }
            else if (temp == 12)
            {
                endingTime = temp + endingTime.Substring(2) + " PM";
            }
            else if (temp == 0)
            {
                temp += 12;
                endingTime = temp + endingTime.Substring(2) + " AM";
            }
            else
            {
                endingTime = temp + endingTime.Substring(2) + " AM";
            }

            return endingTime;
        }

        /// <summary>
        /// Formats the starting time from a 24 hour time to a 12 hour time
        /// with am or pm attached
        /// </summary>
        /// <param name="startingTime"></param>
        /// <returns></returns>
        private static string FormatStartingTime(string startingTime)
        {
            int temp = Int32.Parse(startingTime.Substring(0, 2));
            if (temp > 12)
            {
                // Want to keep 1:00 set at 13:00 for sorting times. 1:00 will always be set before 12:00
                if (temp != 13)
                {
                    temp -= 12;
                }
                startingTime = temp + startingTime.Substring(2) + " PM";
            }
            else if (temp == 12)
            {
                startingTime = temp + startingTime.Substring(2) + " PM";
            }
            else if (temp == 0)
            {
                temp += 12;
                startingTime = temp + startingTime.Substring(2) + " AM";
            }
            else
            {
                startingTime = temp + startingTime.Substring(2) + " AM";
            }

            return startingTime;
        }
    }
}
