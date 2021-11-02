using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AboutController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> IndexStaff()
        {
            return View(await StaffDB.GetAllStaff(_context));
        }

        /// <summary>
        /// Creates a staff member
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(Staff staff)
        {
            await StaffDB.AddStaff(_context, staff);
            return RedirectToAction("IndexStaff");
        }

        /// <summary>
        /// Edits a staff member
        /// </summary>
        /// <param name="id">The id for the staff member</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EditStaff(int id)
        {
            return View(await StaffDB.GetStaffMember(_context, id));
        }

        [HttpPost]
        public async Task<IActionResult> EditStaff(Staff staff)
        {
            await StaffDB.SaveChanges(_context, staff);
            return RedirectToAction("IndexStaff");
        }
    }
}
