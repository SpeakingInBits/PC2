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

        /// <summary>
        /// Deletes a staff member
        /// </summary>
        /// <param name="id">The id of the staff member</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            return View(await StaffDB.GetStaffMember(_context, id));
        }

        [HttpPost]
        [ActionName("DeleteStaff")]
        public async Task<IActionResult> ConfirmDeleteStaff(int id)
        {
            Staff staff = await StaffDB.GetStaffMember(_context, id);
            await StaffDB.Delete(_context, staff);
            return RedirectToAction("IndexStaff");
        }

        public async Task<IActionResult> IndexBoard()
        {
            return View(await BoardDB.GetAllBoardMembers(_context));
        }

        /// <summary>
        /// Creates a board member
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CreateBoard()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBoard(Board board)
        {
            await BoardDB.CreateBoardMember(_context, board);
            return RedirectToAction("IndexBoard");
        }

        /// <summary>
        /// Edits a board member
        /// </summary>
        /// <param name="id">The id of the board member</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EditBoard(int id)
        {
            return View(await BoardDB.GetBoardMember(_context, id));
        }

        [HttpPost]
        public async Task<IActionResult> EditBoard(Board board)
        {
            await BoardDB.EditBoardMember(_context, board);
            return RedirectToAction("IndexBoard");
        }

        /// <summary>
        /// Deletes a board member
        /// </summary>
        /// <param name="id">The id of the board member</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            return View(await BoardDB.GetBoardMember(_context, id));
        }

        [HttpPost]
        [ActionName("DeleteBoard")]
        public async Task<IActionResult> ConfirmDeleteBoard(int id)
        {
            Board board = await BoardDB.GetBoardMember(_context, id);
            await BoardDB.Delete(_context, board);
            return RedirectToAction("IndexBoard");
        }

        public async Task<IActionResult> IndexSteeringCommittee()
        {
            return View(await SteeringCommitteeDB.GetAllSteeringCommittee(_context));
        }

        /// <summary>
        /// Creates a steering committee member
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CreateSteeringCommittee()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSteeringCommittee(SteeringCommittee steeringCommittee)
        {
            await SteeringCommitteeDB.Create(_context, steeringCommittee);
            return RedirectToAction("IndexSteeringCommittee");
        }
    }
}
