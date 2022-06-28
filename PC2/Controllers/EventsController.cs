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

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> EventsAndActivities()
        {
            EventsModel eventsModel = new EventsModel();
            eventsModel.CalendarDate = await CalendarDateDB.GetAllDates(_context);

            // Loop through each date on the Calendar
            for (int i = eventsModel.CalendarDate.Count - 1; i >= 0; i--)
            {
                // Loop through all events for the day, remove from the end of list
                for (int j = eventsModel.CalendarDate[i].Events.Count - 1; j >= 0; j--)
                {
                    // If it is not a PC2 event then it should not be in the list
                    if (eventsModel.CalendarDate[i].Events[j].PC2Event == false)
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

        public async Task<IActionResult> CountyWideEventsAndActivities()
        {
            EventsModel eventsModel = new EventsModel();
            eventsModel.CalendarDate = await CalendarDateDB.GetAllDates(_context);

            // Loop through each date on the Calendar
            for (int i = eventsModel.CalendarDate.Count - 1; i >= 0; i--)
            {
                // Loop through all events for the day, remove from the end of list
                for (int j = eventsModel.CalendarDate[i].Events.Count - 1; j >= 0; j--)
                {
                    // If it is not a County event then it should not be in the list
                    if (eventsModel.CalendarDate[i].Events[j].CountyEvent == false)
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
