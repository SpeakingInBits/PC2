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
            bool getPc2Events = eventType == null;
            EventsModel eventsModel = new()
            {
                IsPC2EventCalendar = getPc2Events,
                CalendarEvents = await CalendarEventDB.GetAllEvents(_context, getPc2Events)
            };

            return View(eventsModel);
        }
    }
}
