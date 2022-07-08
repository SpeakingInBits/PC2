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
            EventsModel eventsModel = new()
            {
                CalendarDate = await CalendarDateDB.GetAllDates(_context),
                IsPC2EventCalendar = (eventType == null)
            };

            // Loop through each date on the Calendar
            for (int i = eventsModel.CalendarDate.Count - 1; i >= 0; i--)
            {
                // Loop through all events for the day, remove from the end of list
                for (int j = eventsModel.CalendarDate[i].Events.Count - 1; j >= 0; j--)
                {
                    // If PC2 calendar is requested, events that are not equal to PC2 will be removed
                    if (eventsModel.CalendarDate[i].Events[j].PC2Event != eventsModel.IsPC2EventCalendar)
                    {
                        eventsModel.CalendarDate[i].Events.RemoveAt(j);
                    }
                }
                // If the events list is empty the date needs to be removed
                if (eventsModel.CalendarDate[i].Events.Count == 0)
                {
                    eventsModel.CalendarDate.RemoveAt(i);
                }
            }
            return View(eventsModel);
        }
    }
}
