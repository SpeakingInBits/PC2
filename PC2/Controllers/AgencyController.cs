using Microsoft.AspNetCore.Authorization;
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
    }
}
