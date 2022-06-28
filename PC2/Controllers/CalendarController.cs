using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;
using System.ComponentModel.DataAnnotations;

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
            List<CalendarDate> dateInfo = await CalendarDateDB.GetAllDates(_context);
            List<CalendarDisplayEventViewModel> calendarData = new();

            // Convert DB data to ViewModel
            for (int i = 0; i < dateInfo.Count; i++)
            {
                calendarData.Add(new()
                {
                    // Convert DateOnly from DB to DateTime (time is ignored)
                    DateOfEvent = dateInfo[i].Date.ToDateTime(new TimeOnly(i))
                });

                for (int j = 0; j < dateInfo[i].Events.Count; j++)
                {
                    calendarData[i].EventsForDate.Add(new()
                    {
                        EventId = dateInfo[i].Events[j].CalendarEventID,
                        Date = dateInfo[i].Date.ToDateTime(new TimeOnly(i)),
                        Description = dateInfo[i].Events[j].EventDescription,
                        IsPc2Event = dateInfo[i].Events[j].PC2Event,
                        IsCountyEvent = dateInfo[i].Events[j].CountyEvent,
                        StartingTime = dateInfo[i].Events[j].StartingTime.ToShortTimeString(),
                        EndingTime = dateInfo[i].Events[j].EndingTime.ToShortTimeString()
                    });
                }
            }

            return View(calendarData);
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            CalendarDate calendarDate = new CalendarDate();
            calendarDate.Date = DateOnly.FromDateTime(model.Date);

            CalendarEvent calendarEvent = new CalendarEvent();

            calendarEvent.StartingTime = TimeOnly.Parse(model.StartingTime);
            calendarEvent.EndingTime = TimeOnly.Parse(model.EndingTime);
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
                return View(model);
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
            CalendarCreateEventViewModel editEvent = new()
            {
                Date = calendarEvent.CalendarDate.Date.ToDateTime(new TimeOnly()),
                Description = calendarEvent.EventDescription,
                EndingTime = calendarEvent.EndingTime.ToShortTimeString(),
                EventId = calendarEvent.CalendarEventID,
                IsCountyEvent = calendarEvent.CountyEvent,
                StartingTime = calendarEvent.StartingTime.ToLongTimeString(),
                IsPc2Event = calendarEvent.PC2Event
            };

            return View(editEvent);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CalendarCreateEventViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            CalendarEvent calendarEvent = new()
            {
                CalendarEventID = model.EventId,
                CountyEvent = model.IsCountyEvent,
                PC2Event = model.IsPc2Event,
                StartingTime = TimeOnly.Parse(model.StartingTime),
                EndingTime = TimeOnly.Parse(model.EndingTime),
                EventDescription = model.Description
            };

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
        /// PK value used to Edit/Delete event
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// The date of the event
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        /// <summary>
        /// Time the event starts
        /// </summary>
        [Required]
        public string StartingTime { get; set; } = null!;

        /// <summary>
        /// Time the event ends
        /// </summary>
        [Required]
        public string EndingTime { get; set; } = null!;

        /// <summary>
        /// Description of the event
        /// </summary>
        [Required]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Is the event a PC2 sponsored event
        /// </summary>
        public bool IsPc2Event { get; set; }

        /// <summary>
        /// Is the event a county sponsored event
        /// </summary>
        public bool IsCountyEvent { get; set; }
    }

    public class CalendarDisplayEventViewModel
    {
        public DateTime DateOfEvent { get; set; }

        public List<CalendarCreateEventViewModel> EventsForDate { get; set; } = new();
    }
}
