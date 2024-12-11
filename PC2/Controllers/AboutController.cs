using System.Drawing.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IWebHostEnvironment _hostingEnvironment;

        private readonly UserManager<IdentityUser> _userManager;

        // Iwebhost environment is used to get the path to the wwwroot folder
        public AboutController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,AdminLite")]
        public async Task<IActionResult> IndexStaff()
        {
            return View(await StaffDB.GetAllStaffForEditing(_context));
        }

        /// <summary>
        /// Creates a staff member
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Admin,AdminLite")]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminLite")]
        public async Task<IActionResult> CreateStaff(Staff staff, string Role)
        {
            if (ModelState.IsValid)
            {
                // Create the Staff member in your custom database table
                await StaffDB.AddStaff(_context, staff);

                // Create the user in Identity
                var user = new IdentityUser
                {
                    UserName = staff.Email,
                    Email = staff.Email
                };

                var result = await _userManager.CreateAsync(user, "DefaultPassword01#"); // Set a default password
                if (result.Succeeded)
                {
                    // Assign the selected role
                    if (!string.IsNullOrEmpty(Role))
                    {
                        await _userManager.AddToRoleAsync(user, Role);
                    }

                    return RedirectToAction("IndexStaff");
                }

                // Handle errors if the user creation fails
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(staff);
        }

        /// <summary>
        /// Edits a staff member
        /// </summary>
        /// <param name="id">The id for the staff member</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Admin,AdminLite")]
        public async Task<IActionResult> EditStaff(int id)
        {
            return View(await StaffDB.GetStaffMember(_context, id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminLite")]
        public async Task<IActionResult> EditStaff(Staff staff)
        {
            if (ModelState.IsValid)
            {
                await StaffDB.SaveChanges(_context, staff);
                return RedirectToAction("IndexStaff");
            }
            
            return View(staff);
        }

        /// <summary>
        /// Deletes a staff member
        /// </summary>
        /// <param name="id">The id of the staff member</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            return View(await StaffDB.GetStaffMember(_context, id));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmDeleteStaff(int id)
        {
            Staff? staff = await StaffDB.GetStaffMember(_context, id);

            if (staff == null)
            {
                // If staff member is not found
                return NotFound(); // Return a NotFound result
            }

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
            if (ModelState.IsValid)
            {
                await BoardDB.CreateBoardMember(_context, board);
                return RedirectToAction("IndexBoard");
            }
            
            return View(board);
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
            if (ModelState.IsValid)
            {
                await BoardDB.EditBoardMember(_context, board);
                return RedirectToAction("IndexBoard");
            }
            
            return View(board);
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
            Board? board = await BoardDB.GetBoardMember(_context, id);

            if (board == null)
            {
                // If board member is not found
                return NotFound(); // Return a NotFound result
            }

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
            if (ModelState.IsValid)
            {
                await SteeringCommitteeDB.Create(_context, steeringCommittee);
                return RedirectToAction("IndexSteeringCommittee");
            }

            return View(steeringCommittee);
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
            if (ModelState.IsValid)
            {
                await SteeringCommitteeDB.EditSteeringCommittee(_context, steeringCommittee);
                return RedirectToAction("IndexSteeringCommittee");
            }
            
            return View(steeringCommittee);
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
            SteeringCommittee? steeringCommittee = await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id);

            if (steeringCommittee == null)
            {
                // If the steering committee member is not found
                return NotFound(); // Return a NotFound result
            }

            await SteeringCommitteeDB.Delete(_context, steeringCommittee);
            return RedirectToAction("IndexSteeringCommittee");
        }

        public async Task<IActionResult> HousingProgramData()
        {
            var data = await _context.HousingProgram.OrderBy(hp => hp.HouseHoldSize).ToListAsync();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> HousingProgramData(HousingProgram model)
        {
            if (ModelState.IsValid)
            { // if all the data is valid, update the database
                HousingProgram entry = new()
                {
                    HouseHoldSize = model.HouseHoldSize,
                    MaximumIncome = model.MaximumIncome,
                    LastUpdated = DateTime.Today
                };
                _context.Entry(entry).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Entry for Household size {entry.HouseHoldSize} updated Successfully";
                return RedirectToAction("HousingProgramData");
            }
            // If model state is not valid, return the view with the model to show validation errors
            List<HousingProgram> data = await _context.HousingProgram.OrderBy(hp => hp.HouseHoldSize).ToListAsync();
            // keep the data from user's input
            data[data.FindIndex(hp => hp.HouseHoldSize == model.HouseHoldSize)].MaximumIncome = model.MaximumIncome;
            TempData["Message"] = $"Maximum income must be a non - negative number at HouseHold size {model.HouseHoldSize}.";
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> UploadNewsletter()
        {
            List<NewsletterFile> newsletterFiles = await NewsletterFileDB.GetAllAsync(_context);
            return View(newsletterFiles);
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
                    Location = $"/PDF/focus-newsletters/{fileName}",
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
            NewsletterFile? newsletter = await NewsletterFileDB.GetFileAsync(_context, id);
            // delete actual file from wwwroot/PDF/focus-newsletters
            if (newsletter != null)
            {
                // actual file name is never changed and object location is never changed when renaming
                string? originalFile = Path.GetFileName(newsletter.Location);
                if (originalFile != null)
                {
                    string filePath = Path.Combine(_hostingEnvironment.WebRootPath, "PDF", "focus-newsletters", originalFile);
                    System.IO.File.Delete(filePath);
                }
                
                // remove from DB
                await NewsletterFileDB.DeleteAsync(_context, newsletter.NewsletterId);
                TempData["Message"] = $"{newsletter.Name} deleted successfully";
            }

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
            NewsletterFile? newsletter = await NewsletterFileDB.GetFileAsync(_context, id);
            
            if (newsletter != null)
            {
                string oldName = newsletter.Name;          
                string newName = Request.Form["Name"];
     
                await NewsletterFileDB.RenameFileAsync(_context, id, newName);
                TempData["Message"] = $"Newsletter {oldName} renamed to {newName}";
            }
            
            return RedirectToAction("UploadNewsletter");
        }
    }
}
