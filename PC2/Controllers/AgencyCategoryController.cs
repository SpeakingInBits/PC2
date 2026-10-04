using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    /// <summary>
    /// Lets Admin and Staff manage the categories agencies are listed under in the Resource Guide
    /// </summary>
    [Authorize(Roles = IdentityHelper.AdminOrStaff)]
    public class AgencyCategoryController : Controller
    {
        public const string DuplicateNameMessage = "A category with this name already exists.";

        private readonly ApplicationDbContext _context;

        public AgencyCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Manage()
        {
            List<AgencyCategoryDisplayViewModel> categories = await AgencyCategoryDB.GetCategoriesWithAgencyCountsAsync(_context);
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AgencyCategory agencyCategory)
        {
            if (ModelState.IsValid && await AgencyCategoryDB.NameExistsAsync(_context, agencyCategory.AgencyCategoryName))
            {
                ModelState.AddModelError(nameof(AgencyCategory.AgencyCategoryName), DuplicateNameMessage);
            }

            if (!ModelState.IsValid)
            {
                return View(agencyCategory);
            }

            agencyCategory.AgencyCategoryName = agencyCategory.AgencyCategoryName.Trim();
            await AgencyCategoryDB.AddCategoryAsync(_context, agencyCategory);
            TempData["Message"] = $"Category \"{agencyCategory.AgencyCategoryName}\" added successfully";
            return RedirectToAction("Manage");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            AgencyCategory? agencyCategory = await AgencyCategoryDB.GetAgencyCategory(_context, id);
            if (agencyCategory == null)
            {
                return NotFound();
            }
            return View(agencyCategory);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AgencyCategory agencyCategory)
        {
            AgencyCategory? existing = await AgencyCategoryDB.GetAgencyCategory(_context, agencyCategory.AgencyCategoryId);
            if (existing == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid
                && await AgencyCategoryDB.NameExistsAsync(_context, agencyCategory.AgencyCategoryName, agencyCategory.AgencyCategoryId))
            {
                ModelState.AddModelError(nameof(AgencyCategory.AgencyCategoryName), DuplicateNameMessage);
            }

            if (!ModelState.IsValid)
            {
                return View(agencyCategory);
            }

            existing.AgencyCategoryName = agencyCategory.AgencyCategoryName.Trim();
            await AgencyCategoryDB.UpdateCategoryAsync(_context, existing);
            TempData["Message"] = $"Category \"{existing.AgencyCategoryName}\" updated successfully";
            return RedirectToAction("Manage");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            AgencyCategory? agencyCategory = await AgencyCategoryDB.GetAgencyCategory(_context, id);
            if (agencyCategory == null)
            {
                return NotFound();
            }

            ViewData["AgencyCount"] = await AgencyCategoryDB.GetAgencyCountAsync(_context, id);
            return View(agencyCategory);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            try
            {
                AgencyCategory? agencyCategory = await AgencyCategoryDB.GetAgencyCategory(_context, id);
                if (agencyCategory != null)
                {
                    await AgencyCategoryDB.DeleteCategoryAsync(_context, id);
                    TempData["Message"] = $"Category \"{agencyCategory.AgencyCategoryName}\" deleted successfully";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting category: {ex.Message}";
            }

            return RedirectToAction("Manage");
        }
    }
}
