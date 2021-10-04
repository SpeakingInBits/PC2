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

        public async Task<IActionResult> ResourceGuide(int categoryID, int yPosition)
        {
            ResourceGuideModel resourceGuide = new ResourceGuideModel();
            if (categoryID != 0)
            {
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, categoryID);
                resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, categoryID);
            }

            resourceGuide.AgencyCategories = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            resourceGuide.AgenciesForDataList = await AgencyDB.GetAllAgencyAsync(_context);

            return View(resourceGuide);
        }
    }
}
