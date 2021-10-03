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

        public async Task<IActionResult> ResourceGuide(int categoryID, int yPos)
        {
            List<Agency> agencies = new List<Agency>();
            AgencyCategory agencyCategory = new AgencyCategory();
            if (categoryID != 0)
            {
                agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, categoryID);
                agencyCategory = await AgencyCategoryDB.GetAgencyCategory(_context, categoryID);
            }

            ViewData["Agency"] = agencies;
            ViewData["AgencyCategory"] = agencyCategory;
            ViewData["YPosition"] = yPos;

            List<AgencyCategory> agencyCategories = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            return View(agencyCategories);
        }
    }
}
