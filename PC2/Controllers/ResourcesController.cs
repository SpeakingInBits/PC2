﻿using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TelemetryClient _telemetryClient;

        public ResourcesController(ApplicationDbContext context, TelemetryClient telemetryClient = null)
        {
            _context = context;
            _telemetryClient = telemetryClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Searches for agencies with the category the user selected from the table of categories.
        /// </summary>
        /// <param name="categoryID">The categoryID of the user's selection.</param>
        [HttpGet]
        public async Task<IActionResult> ResourceGuide(int categoryID)
        {
            ResourceGuideModel resourceGuide = new ResourceGuideModel();
            if (categoryID != 0)
            {
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, categoryID);
                resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, categoryID);
            }

            await AgencyDB.GetDataForDataLists(_context, resourceGuide);

            return View(resourceGuide);
        }

        /// <summary>
        /// Searches for agencies that match the criteria the user searched for.
        /// </summary>
        /// <param name="searchModel">A model containing what the user searched for.</param>
        [HttpPost]
        public async Task<IActionResult> ResourceGuide(ResourceGuideModel searchModel)
        {
            ResourceGuideModel resourceGuide = new()
            {
                UserSearchedByCityOrService = searchModel.UserSearchedByCityOrService,
                UserSearchedByAgency = searchModel.UserSearchedByAgency
            };

            if (!string.IsNullOrEmpty(searchModel.UserSearchedByAgency))
            {
                if (searchModel.SearchedAgency != null)
                {
                    _telemetryClient.TrackEvent("ResourceGuideSearch", 
                        new Dictionary<string, string>{
                        { "SearchType", "Agency" },
                        { "SearchTerm", searchModel.SearchedAgency }
                    });
                    resourceGuide.Agencies = await AgencyDB.GetAgenciesByName(_context, searchModel.SearchedAgency);
                }
            }
            else if (!string.IsNullOrEmpty(searchModel.UserSearchedByCityOrService))
            {
                if (searchModel.SearchedCategory != null
                && searchModel.SearchedCity != null)
                {
                    _telemetryClient.TrackEvent("ResourceGuideSearch",
                        new Dictionary<string, string>{
                        { "SearchType", "CityAndCategory" },
                        { "SearchTerm", $"{searchModel.SearchedCity} - {searchModel.SearchedCategory}" }
                    });
                    resourceGuide.Agencies = await AgencyDB.GetAgenciesByCategoryAndCity(_context,
                        searchModel.SearchedCategory, searchModel.SearchedCity);
                }
                else if (searchModel.SearchedCategory != null)
                {
                    _telemetryClient.TrackEvent("ResourceGuideSearch",
                        new Dictionary<string, string>{
                        { "SearchType", "Service" },
                        { "SearchTerm", $"{searchModel.SearchedCategory}" }
                    });
                    resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, searchModel.SearchedCategory);
                    resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, resourceGuide.Category.AgencyCategoryId);
                }
                else if (searchModel.SearchedCity != null)
                {
                    _telemetryClient.TrackEvent("ResourceGuideSearch",
                        new Dictionary<string, string>{
                        { "SearchType", "City" },
                        { "SearchTerm", $"{searchModel.SearchedCity}" }
                    });
                    resourceGuide.CurrentCity = searchModel.SearchedCity;
                    resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, searchModel.SearchedCity);
                }
            }
            
            await AgencyDB.GetDataForDataLists(_context, resourceGuide);
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
