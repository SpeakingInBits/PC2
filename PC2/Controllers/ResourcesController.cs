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

        public async Task<IActionResult> ResourceGuide(ResourceGuideModel searchModel)
        {
            ResourceGuideModel resourceGuide = new ResourceGuideModel();
            if (searchModel.SearchedAgency != null)
            {
                resourceGuide.Agencies = await AgencyDB.GetAgenciesByName(_context, searchModel.SearchedAgency);
            }
            if (searchModel.SearchedCategory != null)
            {
                resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, searchModel.SearchedCategory);
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, resourceGuide.Category.AgencyCategoryId);
            }
            if (searchModel.SearchedCity != null)
            {
                resourceGuide.CurrentCity = searchModel.SearchedCity;
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, searchModel.SearchedCity);
            }

            resourceGuide.AgencyCategories = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            resourceGuide.AgenciesForDataList = await AgencyDB.GetDistinctAgenciesAsync(_context);
            resourceGuide.YPos = searchModel.YPos;
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
