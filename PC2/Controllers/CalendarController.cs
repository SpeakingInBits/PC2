﻿using Microsoft.AspNetCore.Authorization;
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
            List<CalendarEvent> calendarEvents = await CalendarEventDB.GetAllEvents(_context);
            return View(calendarEvents);
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

            CalendarEvent newEvent = new()
            {
                DateOfEvent = DateOnly.FromDateTime(model.DateOfEvent),
                StartingTime = TimeOnly.Parse(model.StartingTime),
                EndingTime = TimeOnly.Parse(model.EndingTime),
                EventDescription = model.Description
            };

            if (model.IsPc2Event)
            {
                newEvent.PC2Event = true;
            }
            else if (model.IsCountyEvent)
            {
                newEvent.CountyEvent = true;
            }
            else
            {
                ModelState.AddModelError(string.Empty, "You must check a box for type of event");
                return View(model);
            }

            await CalendarEventDB.AddEvent(_context, newEvent);

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
            CalendarEvent? calendarEvent = await CalendarEventDB.GetEvent(_context, id);

            if (calendarEvent == null)
            {
                return NotFound();
            }

            CalendarCreateEventViewModel editEvent = new()
            {
                DateOfEvent = calendarEvent.DateOfEvent.ToDateTime(new TimeOnly()),
                Description = calendarEvent.EventDescription,
                EndingTime = calendarEvent.EndingTime.ToString("HH:mm"),
                EventId = calendarEvent.CalendarEventID,
                IsCountyEvent = calendarEvent.CountyEvent,
                StartingTime = calendarEvent.StartingTime.ToString("HH:mm"),
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
                EventDescription = model.Description,
                DateOfEvent = DateOnly.FromDateTime(model.DateOfEvent)
            };

            bool success = await CalendarEventDB.UpdateEvent(_context, calendarEvent);

            if (!success)
            {
                TempData["UpdateFailed"] = "An error occurred updating the event";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            await CalendarEventDB.DeleteEvent(_context, id);
            TempData["EventDeleted"] = true;
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
        public DateTime DateOfEvent { get; set; }

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
}
