using Microsoft.AspNetCore.Mvc;
using PC2.Data;

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

        public async Task<IActionResult> Create()
        {
            return View();
        }
    }
}
