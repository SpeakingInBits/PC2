﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class AgencyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AgencyController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int? id)
        {
            int pageNum = id ?? 1;
            const int PageSize = 40;
            ViewData["CurrentPage"] = pageNum;

            List<Agency> totalAgencies = await AgencyDB.GetAllAgenciesAsync(_context);
            ViewData["MaxPage"] = (int)Math.Ceiling((double)totalAgencies.Count / PageSize);

            List<Agency> agencies = await AgencyDB.GetAllAgenciesAsync(_context, PageSize, pageNum);
            return View(agencies);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["AgencyCategories"] = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Agency agency, string services)
        {
            if (ModelState.IsValid)
            {
                string[] serviceArray = JsonConvert.DeserializeObject<string[]>(services);
                for (int i = 0; i < serviceArray.Length; i++)
                {
                    agency.AgencyCategories.Add(await AgencyCategoryDB.GetAgencyCategory(_context, serviceArray[i]));
                }
                await AgencyDB.AddAgencyAsync(_context, agency);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edits an agency
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            Agency agency = await AgencyDB.GetAgencyAsync(_context, id);
            ViewData["ExistingServices"] = agency.AgencyCategories;
            ViewData["Categories"] = await AgencyCategoryDB.GetAgencyCategoriesAsync(_context);
            return View(agency);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Agency agency, string servicesRemoved, string servicesAdded)
        {
            if (ModelState.IsValid)
            {
                string[] serviceArray = JsonConvert.DeserializeObject<string[]>(servicesRemoved);
                List<AgencyCategory> removedCategories = new List<AgencyCategory>();
                for (int i = 0; i < serviceArray.Length; i++)
                {
                    AgencyCategory temp = await AgencyCategoryDB.GetAgencyCategory(_context, serviceArray[i]);
                    removedCategories.Add(temp);
                    agency.AgencyCategories.Add(temp);
                }
                serviceArray = JsonConvert.DeserializeObject<string[]>(servicesAdded);
                for (int i = 0; i < serviceArray.Length; i++)
                {
                    AgencyCategory temp = await AgencyCategoryDB.GetAgencyCategory(_context, serviceArray[i]);
                    if (!agency.AgencyCategories.Contains(temp))
                    {
                        agency.AgencyCategories.Add(temp);
                    }
                }
                await AgencyDB.UpdateAgencyAsync(_context, agency, removedCategories);
            }
            return RedirectToAction("Index");
        }
    }
}
