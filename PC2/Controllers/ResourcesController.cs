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

        public async Task<IActionResult> ResourceGuide(int categoryID, int yPosition, string? agencyName, string? agencyCategory, 
            string? city)
        {
            ResourceGuideModel resourceGuide = new ResourceGuideModel();
            if (categoryID != 0)
            {
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, categoryID);
                resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, categoryID);
            }
            if (agencyName != null)
            {
                resourceGuide.Agencies = await AgencyDB.GetAgenciesByName(_context, agencyName);
            }
            if (agencyCategory != null)
            {
                resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, agencyCategory);
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, resourceGuide.Category.AgencyCategoryId);
            }
            if (city != null)
            {
                resourceGuide.CurrentCity = city;
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, city);
            }

            resourceGuide.AgencyCategories = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            resourceGuide.AgenciesForDataList = await AgencyDB.GetDistinctAgenciesAsync(_context);
            resourceGuide.YPos = yPosition;
            resourceGuide.City = await AgencyDB.GetAllCities(_context);

            return View(resourceGuide);
        }

        public async Task<IActionResult> Details(int id)
        {
            Agency agency = await AgencyDB.GetAgencyAsync(_context, id);
            return View(agency);
        }
            
        public IActionResult DisabilityAwareness()
        {
            return View();
        }

        public IActionResult ResourceLinks()
        {
            return View();
        }

        public IActionResult AgeSpecificIssues()
        {
            return View();
        }

        public IActionResult LegislativeLinksAndEvents()
        {
            return View();
        }

        public IActionResult EmergencyPreparedness()
        {
            return View();
        }

        public IActionResult VirtualCloset()
        {
            return View();
        }

        public IActionResult FocusNewsletters()
        {
            return View();
        }
    }
}
