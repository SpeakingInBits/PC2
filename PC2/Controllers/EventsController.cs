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
    }
}
