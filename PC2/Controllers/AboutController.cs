﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IWebHostEnvironment _hostingEnvironment;

        // Iwebhost environment is used to get the path to the wwwroot folder
        public AboutController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<IActionResult> IndexStaff()
        {
            return View(await StaffDB.GetAllStaffForEditing(_context));
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
            return View(await BoardDB.GetAllBoardMembersForEditing(_context));
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

        /// <summary>
        /// Gets a steering committee member by id
        /// </summary>
        /// <param name="id">The id of the steering committee member</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EditSteeringCommittee(int id)
        {
            return View(await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id));
        }

        [HttpPost]
        public async Task<IActionResult> EditSteeringCommittee(SteeringCommittee steeringCommittee)
        {
            await SteeringCommitteeDB.EditSteeringCommittee(_context, steeringCommittee);
            return RedirectToAction("IndexSteeringCommittee");
        }

        /// <summary>
        /// Deletes a steering committee member by id
        /// </summary>
        /// <param name="id">The id of the member</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DeleteSteeringCommittee(int id)
        {
            return View(await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id));
        }

        [HttpPost]
        [ActionName("DeleteSteeringCommittee")]
        public async Task<IActionResult> ConfirmDeleteSteeringCommittee(int id)
        {
            SteeringCommittee steeringCommittee = await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id);
            await SteeringCommitteeDB.Delete(_context, steeringCommittee);
            return RedirectToAction("IndexSteeringCommittee");
        }

        public async Task<IActionResult> HousingProgramData()
        {
            var data = await _context.HousingProgram.OrderBy(hp => hp.HouseHoldSize).ToListAsync();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> HousingProgramData(IFormCollection form)
        {
            HousingProgram entry = new()
            {
                HouseHoldSize = int.Parse(form["HouseHoldSize"]),
                MaximumIncome = double.Parse(form["MaximumIncome"]),
                LastUpdated = DateTime.Today
            };
            _context.Entry(entry).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Entry for Household size {entry.HouseHoldSize} updated Successfully";
            return RedirectToAction("HousingProgramData");
        }

        [HttpGet]
        public async Task<IActionResult> UploadNewsletter()
        {
            ViewData["NewsletterFiles"] = await NewsletterFileDB.GetAllAsync(_context);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadNewsletter(IFormFile userFile)
        {
            if (userFile != null)
            {
                // set path to wwwroot/PDF/focus-newsletter/file.pdf
                string directory = Path.Combine(_hostingEnvironment.WebRootPath, "PDF", "focus-newsletters");
                string fileName = Path.GetFileName(userFile.FileName);
                string filePath = Path.Combine(directory, fileName);

                // copy physical file to path
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    userFile.CopyTo(fileStream);
                }
                TempData["Message"] = $"{fileName} uploaded successfully";

                NewsletterFile newsLetterFile = new()
                {
                    Name = fileName,
                    Location = $"~/PDF/focus-newsletters/{fileName}",
                };

                // add newsletterFile to the DB
                await NewsletterFileDB.AddAsync(_context, newsLetterFile);
            }

            return RedirectToAction("UploadNewsletter");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNewsletter(int id)
        {
            return View(await NewsletterFileDB.GetFileAsync(_context, id));
        }

        [HttpPost]
        [ActionName("DeleteNewsletter")]
        public async Task<IActionResult> ConfirmDeleteNewsletter(int id)
        {
            NewsletterFile newsletter = await NewsletterFileDB.GetFileAsync(_context, id);
            // delete actual file from wwwroot/PDF/focus-newsletters
            if (newsletter.Name != null)
            {
                // actual file name is never changed and object location is never changed when renaming
                string? originalFile = Path.GetFileName(newsletter.Location);
                if (originalFile != null)
                {
                    string filePath = Path.Combine(_hostingEnvironment.WebRootPath, "PDF", "focus-newsletters", originalFile);
                    System.IO.File.Delete(filePath);
                }
            }

            // remove from DB
            await NewsletterFileDB.DeleteAsync(_context, newsletter.NewsletterId);
            TempData["Message"] = $"{newsletter.Name} deleted successfully";

            return RedirectToAction("UploadNewsletter");
        }

        [HttpGet]
        public async Task<IActionResult> RenameNewsletter(int id)
        {
            {
                return View(await NewsletterFileDB.GetFileAsync(_context, id));
            }
        }

        [HttpPost]
        [ActionName("RenameNewsletter")]
        public async Task<IActionResult> ConfirmRenameNewsletter(int id)
        {
            NewsletterFile newsletter = await NewsletterFileDB.GetFileAsync(_context, id);
            string? oldName = newsletter.Name;          
            string newName = Request.Form["Name"];
     
            await NewsletterFileDB.RenameFileAsync(_context, id, newName);
            TempData["Message"] = $"Newsletter {oldName} renamed to {newName}";
            
            return RedirectToAction("UploadNewsletter");
        }
    }
}
