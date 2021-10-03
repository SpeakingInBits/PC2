using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResourcesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ResourceGuide(int categoryID)
        {
            List<Agency> agencies = new List<Agency>();
            if (categoryID != 0)
            {
                agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, (int)categoryID);
            }

            ViewData["Agency"] = agencies;

            List<AgencyCategory> agencyCategories = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            return View(agencyCategories);
        }
    }
}
