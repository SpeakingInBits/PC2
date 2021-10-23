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
            EventsModel events = new EventsModel();
            events.CalendarDate = await CalendarDateDB.GetAllDates(_context);
            
            return View(events);
        }

        public async Task<IActionResult> EventsAndActivities()
        {
            EventsModel eventsModel = new EventsModel();
            eventsModel.CalendarDate = await CalendarDateDB.GetAllDates(_context);
            for (int i = 0; i < eventsModel.CalendarDate.Count; i++)
            {
                for (int j = 0; j < eventsModel.CalendarDate[i].Events.Count; j++)
                {
                    // If it is not a PC2 event then it should not be in the list
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

        public async Task<IActionResult> CountyWideEventsAndActivities()
        {
            EventsModel eventsModel = new EventsModel();
            eventsModel.CalendarDate = await CalendarDateDB.GetAllDates(_context);
            for (int i = 0; i < eventsModel.CalendarDate.Count; i++)
            {
                for (int j = 0; j < eventsModel.CalendarDate[i].Events.Count; j++)
                {
                    // If it is not a county event then it should not be in the list
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
    }
}
