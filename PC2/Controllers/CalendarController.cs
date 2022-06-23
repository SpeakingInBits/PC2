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
        public async Task<IActionResult> Create(CalendarCreateEventViewModel model)
        {
            CalendarDate calendarDate = new CalendarDate();
            calendarDate.Date = model.Date;

            CalendarEvent calendarEvent = new CalendarEvent();

            calendarEvent.StartingTime = model.StartingTime;
            calendarEvent.EndingTime = model.EndingTime;
            calendarEvent.EventDescription = model.Description;

            if (model.IsPc2Event)
            {
                calendarEvent.PC2Event = true;
            }
            else if (model.IsCountyEvent)
            {
                calendarEvent.CountyEvent = true;
            }
            else
            {
                ModelState.AddModelError(String.Empty, "You must check a box for type of event");
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
            return View(calendarEvent);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CalendarEvent calendarEvent)
        {
            bool success = await CalendarEventDB.UpdateEvent(_context, calendarEvent);

            if (!success)
            {
                TempData["UpdateFailed"] = "An error occurred updating the event";
            }

            return RedirectToAction("Index");
        }
    }
    public class CalendarCreateEventViewModel
    {
        /// <summary>
        /// The date of the event
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Time the event starts
        /// </summary>
        public TimeOnly StartingTime { get; set; }

        /// <summary>
        /// Time the event ends
        /// </summary>
        public TimeOnly EndingTime { get; set; }

        /// <summary>
        /// Description of the event
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Is the event a PC2 sponsored event
        /// </summary>
        public bool IsPc2Event { get; set; }

        /// <summary>
        /// Is the event a county sponsored event
        /// </summary>
        public bool IsCountyEvent { get; set; }
    }
}
