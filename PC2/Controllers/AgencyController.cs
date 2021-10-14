using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    public class AgencyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AgencyController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            List<Agency> agencies = await AgencyDB.GetAllAgenciesAsync(_context);
            return View(agencies);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}
