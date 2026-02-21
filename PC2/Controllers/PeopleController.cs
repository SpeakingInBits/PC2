using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;
using PC2.Models.ViewModels;
using PC2.Services;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class PeopleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobUploader _azureBlobUploader;
        private readonly ImageService _imageService;

        public PeopleController(ApplicationDbContext context, AzureBlobUploader azureBlobUploader, ImageService imageService)
        {
            _context = context;
            _azureBlobUploader = azureBlobUploader;
            _imageService = imageService;
        }

        // ---- Staff ----

        public async Task<IActionResult> IndexStaff()
        {
            return View(await StaffDB.GetAllStaffForEditing(_context));
        }

        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View(new CreateStaffViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = new Staff
                {
                    Name = model.Name,
                    Title = model.Title,
                    Phone = model.Phone,
                    Extension = model.Extension,
                    Email = model.Email,
                    PriorityOrder = model.PriorityOrder
                };

                await HandlePhotoUpload(model.PhotoFile, staff);
                await StaffDB.AddStaff(_context, staff);
                return RedirectToAction(nameof(IndexStaff));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditStaff(int id)
        {
            var staff = await StaffDB.GetStaffMember(_context, id);
            if (staff == null)
            {
                return NotFound();
            }

            var model = new EditStaffViewModel
            {
                ID = staff.ID,
                Name = staff.Name,
                Title = staff.Title,
                Phone = staff.Phone,
                Extension = staff.Extension,
                Email = staff.Email,
                CurrentImageUrl = staff.ImageUrl,
                PriorityOrder = staff.PriorityOrder
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditStaff(EditStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = await StaffDB.GetStaffMember(_context, model.ID);
                if (staff == null)
                {
                    return NotFound();
                }

                staff.Name = model.Name;
                staff.Title = model.Title;
                staff.Phone = model.Phone;
                staff.Extension = model.Extension;
                staff.Email = model.Email;
                staff.PriorityOrder = model.PriorityOrder;

                if (model.RemovePhoto)
                {
                    await RemoveStaffPhoto(staff);
                }
                else if (model.PhotoFile != null)
                {
                    await HandlePhotoUpload(model.PhotoFile, staff, model.ID);
                }

                await StaffDB.SaveChanges(_context, staff);
                return RedirectToAction(nameof(IndexStaff));
            }

            model.CurrentImageUrl = (await StaffDB.GetStaffMember(_context, model.ID))?.ImageUrl;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            return View(await StaffDB.GetStaffMember(_context, id));
        }

        [HttpPost, ActionName("DeleteStaff")]
        public async Task<IActionResult> ConfirmDeleteStaff(int id)
        {
            Staff? staff = await StaffDB.GetStaffMember(_context, id);
            if (staff == null)
            {
                return NotFound();
            }

            await StaffDB.Delete(_context, staff);
            return RedirectToAction(nameof(IndexStaff));
        }

        // ---- Board ----

        public async Task<IActionResult> IndexBoard()
        {
            return View(await BoardDB.GetAllBoardMembersForEditing(_context));
        }

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
                return RedirectToAction(nameof(IndexBoard));
            }
            return View(board);
        }

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
                return RedirectToAction(nameof(IndexBoard));
            }
            return View(board);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            return View(await BoardDB.GetBoardMember(_context, id));
        }

        [HttpPost, ActionName("DeleteBoard")]
        public async Task<IActionResult> ConfirmDeleteBoard(int id)
        {
            Board? board = await BoardDB.GetBoardMember(_context, id);
            if (board == null)
            {
                return NotFound();
            }

            await BoardDB.Delete(_context, board);
            return RedirectToAction(nameof(IndexBoard));
        }

        // ---- Steering Committee ----

        public async Task<IActionResult> IndexSteeringCommittee()
        {
            return View(await SteeringCommitteeDB.GetAllSteeringCommittee(_context));
        }

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
                return RedirectToAction(nameof(IndexSteeringCommittee));
            }
            return View(steeringCommittee);
        }

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
                return RedirectToAction(nameof(IndexSteeringCommittee));
            }
            return View(steeringCommittee);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSteeringCommittee(int id)
        {
            return View(await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id));
        }

        [HttpPost, ActionName("DeleteSteeringCommittee")]
        public async Task<IActionResult> ConfirmDeleteSteeringCommittee(int id)
        {
            SteeringCommittee? steeringCommittee = await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id);
            if (steeringCommittee == null)
            {
                return NotFound();
            }

            await SteeringCommitteeDB.Delete(_context, steeringCommittee);
            return RedirectToAction(nameof(IndexSteeringCommittee));
        }

        // ---- Photo helpers ----

        private async Task HandlePhotoUpload(IFormFile? photoFile, Staff staff, int? staffId = null)
        {
            if (photoFile == null || photoFile.Length == 0) return;

            try
            {
                if (!ImageService.IsValidImageFile(photoFile))
                {
                    throw new InvalidOperationException("Please upload a valid image file (JPEG, PNG, GIF, or BMP).");
                }

                if (staffId.HasValue && !string.IsNullOrEmpty(staff.ImageUrl))
                {
                    await RemoveStaffPhoto(staff);
                }

                var safeFileName = ImageService.GetSafeImageFileName(photoFile.FileName, staffId ?? 0);
                using var resizedImageStream = await _imageService.ResizeImageAsync(photoFile.OpenReadStream());
                var resizedFormFile = new FormFileFromStream(resizedImageStream, safeFileName, photoFile.ContentType);
                staff.ImageUrl = await _azureBlobUploader.UploadFileAsync(resizedFormFile, safeFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling image upload: {ex.Message}");
            }
        }

        private async Task RemoveStaffPhoto(Staff staff)
        {
            if (string.IsNullOrEmpty(staff.ImageUrl)) return;

            try
            {
                var fileName = staff.ImageUrl.Split('/').LastOrDefault();
                if (!string.IsNullOrEmpty(fileName))
                {
                    await _azureBlobUploader.DeleteFileAsync(fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting photo: {ex.Message}");
            }

            staff.ImageUrl = null;
        }
    }
}
