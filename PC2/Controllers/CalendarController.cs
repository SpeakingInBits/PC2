using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Sets date to today at midnight
            //DateTime today = DateTime.Today;
            return View();
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

        private static string FormatEndingTime(string endingTime)
        {
            int temp = Int32.Parse(endingTime.Substring(0, 2));
            if (temp > 12)
            {
                temp -= 12;
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

        private static string FormatStartingTime(string startingTime)
        {
            int temp = Int32.Parse(startingTime.Substring(0, 2));
            if (temp > 12)
            {
                temp -= 12;
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
