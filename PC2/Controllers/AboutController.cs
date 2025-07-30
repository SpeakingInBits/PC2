using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobUploader _azureBlobUploader;

        private readonly IWebHostEnvironment _hostingEnvironment;

        // Iwebhost environment is used to get the path to the wwwroot folder
        public AboutController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment, AzureBlobUploader azureBlobUploader)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _azureBlobUploader = azureBlobUploader;
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
            if (ModelState.IsValid)
            {
                await StaffDB.AddStaff(_context, staff);
                return RedirectToAction("IndexStaff");
            }
            return View(staff);
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
        public async Task<IActionResult> DeleteStaff(int id)
        {
            return View(await StaffDB.GetStaffMember(_context, id));
        }

        [HttpPost]
        [ActionName("DeleteStaff")]
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
                try
                {
                    // Upload the file to BLOB storage
                    string filePath = await _azureBlobUploader.UploadFileAsync(userFile, userFile.FileName);
                    TempData["Message"] = $"{userFile.FileName} uploaded successfully";

                    NewsletterFile newsLetterFile = new()
                    {
                        Name = userFile.FileName,
                        Location = filePath,
                    };

                    // add newsletterFile to the DB
                    await NewsletterFileDB.AddAsync(_context, newsLetterFile);
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Error uploading file: {ex.Message}";
                    List<NewsletterFile> newsletterFiles = await NewsletterFileDB.GetAllAsync(_context);
                    return View(newsletterFiles);
                }
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
            try
            {
                NewsletterFile? newsletter = await NewsletterFileDB.GetFileAsync(_context, id);
                // delete actual file from wwwroot/PDF/focus-newsletters
                if (newsletter != null)
                {
                    // actual file name is never changed and object location is never changed when renaming
                    string? originalFile = newsletter.Location?.Split('/').LastOrDefault(); // Get the file name from the end of the URL
                    if (originalFile != null)
                    {
                        bool isDeleted = await _azureBlobUploader.DeleteFileAsync(originalFile); // Delete the file from Azure Blob Storage
                    }

                    // remove from DB
                    await NewsletterFileDB.DeleteAsync(_context, newsletter.NewsletterId);
                    TempData["Message"] = $"{newsletter.Name} deleted successfully";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting file: {ex.Message}";
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
