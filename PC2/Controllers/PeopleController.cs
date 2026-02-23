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
        private readonly ILogger<PeopleController> _logger;

        public PeopleController(ApplicationDbContext context, AzureBlobUploader azureBlobUploader, ImageService imageService, ILogger<PeopleController> logger)
        {
            _context = context;
            _azureBlobUploader = azureBlobUploader;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(PersonType type)
        {
            ViewData["PersonType"] = type;
            IEnumerable<PersonViewModel> people = type switch
            {
                PersonType.Staff => (await StaffDB.GetAllStaffForEditing(_context)).Select(PersonViewModel.FromStaff),
                PersonType.Board => (await BoardDB.GetAllBoardMembersForEditing(_context)).Select(PersonViewModel.FromBoard),
                PersonType.SteeringCommittee => (await SteeringCommitteeDB.GetAllSteeringCommittee(_context)).Select(PersonViewModel.FromSteeringCommittee),
                _ => []
            };
            return View(people);
        }

        [HttpGet]
        public IActionResult Create(PersonType type)
        {
            return View(new PersonViewModel { Type = type });
        }

        [HttpPost]
        public async Task<IActionResult> Create(PersonViewModel model)
        {
            ValidateTypeSpecificFields(model);

            if (ModelState.IsValid)
            {
                switch (model.Type)
                {
                    case PersonType.Staff:
                        var staff = new Staff
                        {
                            Name = model.Name,
                            Title = model.Title,
                            Phone = model.Phone,
                            Extension = model.Extension,
                            Email = model.Email!,
                            PriorityOrder = model.PriorityOrder
                        };
                        await HandlePhotoUpload(model.PhotoFile, staff);
                        await StaffDB.AddStaff(_context, staff);
                        break;

                    case PersonType.Board:
                        var board = new Board
                        {
                            Name = model.Name,
                            Title = model.Title,
                            MembershipStart = model.MembershipStart!,
                            PriorityOrder = model.PriorityOrder
                        };
                        await HandlePhotoUpload(model.PhotoFile, board);
                        await BoardDB.CreateBoardMember(_context, board);
                        break;

                    case PersonType.SteeringCommittee:
                        var sc = new SteeringCommittee
                        {
                            Name = model.Name,
                            Title = model.Title,
                            PriorityOrder = model.PriorityOrder
                        };
                        await HandlePhotoUpload(model.PhotoFile, sc);
                        await SteeringCommitteeDB.Create(_context, sc);
                        break;
                }
                return RedirectToAction(nameof(Index), new { type = model.Type });
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, PersonType type)
        {
            PersonViewModel? model = type switch
            {
                PersonType.Staff => await StaffDB.GetStaffMember(_context, id) is Staff s
                    ? PersonViewModel.FromStaff(s) : null,
                PersonType.Board => await BoardDB.GetBoardMember(_context, id) is Board b
                    ? PersonViewModel.FromBoard(b) : null,
                PersonType.SteeringCommittee => await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id) is SteeringCommittee sc
                    ? PersonViewModel.FromSteeringCommittee(sc) : null,
                _ => null
            };

            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PersonViewModel model)
        {
            ValidateTypeSpecificFields(model);

            if (ModelState.IsValid)
            {
                switch (model.Type)
                {
                    case PersonType.Staff:
                        var staff = await StaffDB.GetStaffMember(_context, model.ID);
                        if (staff == null) return NotFound();
                        staff.Name = model.Name;
                        staff.Title = model.Title;
                        staff.Phone = model.Phone;
                        staff.Extension = model.Extension;
                        staff.Email = model.Email!;
                        staff.PriorityOrder = model.PriorityOrder;
                        if (model.RemovePhoto) await RemovePersonPhoto(staff);
                        else if (model.PhotoFile != null) await HandlePhotoUpload(model.PhotoFile, staff, model.ID);
                        await StaffDB.SaveChanges(_context, staff);
                        break;

                    case PersonType.Board:
                        var board = await BoardDB.GetBoardMember(_context, model.ID);
                        if (board == null) return NotFound();
                        board.Name = model.Name;
                        board.Title = model.Title;
                        board.MembershipStart = model.MembershipStart!;
                        board.PriorityOrder = model.PriorityOrder;
                        if (model.RemovePhoto) await RemovePersonPhoto(board);
                        else if (model.PhotoFile != null) await HandlePhotoUpload(model.PhotoFile, board, model.ID);
                        await BoardDB.EditBoardMember(_context, board);
                        break;

                    case PersonType.SteeringCommittee:
                        var sc = await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, model.ID);
                        if (sc == null) return NotFound();
                        sc.Name = model.Name;
                        sc.Title = model.Title;
                        sc.PriorityOrder = model.PriorityOrder;
                        if (model.RemovePhoto) await RemovePersonPhoto(sc);
                        else if (model.PhotoFile != null) await HandlePhotoUpload(model.PhotoFile, sc, model.ID);
                        await SteeringCommitteeDB.EditSteeringCommittee(_context, sc);
                        break;
                }
                return RedirectToAction(nameof(Index), new { type = model.Type });
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, PersonType type)
        {
            PersonViewModel? model = type switch
            {
                PersonType.Staff => await StaffDB.GetStaffMember(_context, id) is Staff s
                    ? PersonViewModel.FromStaff(s) : null,
                PersonType.Board => await BoardDB.GetBoardMember(_context, id) is Board b
                    ? PersonViewModel.FromBoard(b) : null,
                PersonType.SteeringCommittee => await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id) is SteeringCommittee sc
                    ? PersonViewModel.FromSteeringCommittee(sc) : null,
                _ => null
            };

            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> ConfirmDelete(int id, PersonType type)
        {
            switch (type)
            {
                case PersonType.Staff:
                    var staff = await StaffDB.GetStaffMember(_context, id);
                    if (staff == null) return NotFound();
                    await StaffDB.Delete(_context, staff);
                    break;

                case PersonType.Board:
                    var board = await BoardDB.GetBoardMember(_context, id);
                    if (board == null) return NotFound();
                    await BoardDB.Delete(_context, board);
                    break;

                case PersonType.SteeringCommittee:
                    var sc = await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id);
                    if (sc == null) return NotFound();
                    await SteeringCommitteeDB.Delete(_context, sc);
                    break;
            }
            return RedirectToAction(nameof(Index), new { type });
        }

        private void ValidateTypeSpecificFields(PersonViewModel model)
        {
            if (model.Type == PersonType.Staff && string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(PersonViewModel.Email), "Email is required for staff members.");
            if (model.Type == PersonType.Board && string.IsNullOrWhiteSpace(model.MembershipStart))
                ModelState.AddModelError(nameof(PersonViewModel.MembershipStart), "Membership start year is required.");
        }

        private async Task HandlePhotoUpload(IFormFile? photoFile, People person, int? personId = null)
        {
            if (photoFile == null || photoFile.Length == 0) return;

            try
            {
                if (!ImageService.IsValidImageFile(photoFile))
                    throw new InvalidOperationException("Please upload a valid image file (JPEG, PNG, GIF, or BMP).");

                if (personId.HasValue && !string.IsNullOrEmpty(person.ImageUrl))
                    await RemovePersonPhoto(person);

                var safeFileName = ImageService.GetSafeImageFileName(photoFile.FileName, personId ?? 0);
                using var resizedImageStream = await _imageService.ResizeImageAsync(photoFile.OpenReadStream(), 350, 350);
                var resizedFormFile = new FormFileFromStream(resizedImageStream, safeFileName, photoFile.ContentType);
                person.ImageUrl = await _azureBlobUploader.UploadFileAsync(resizedFormFile, safeFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling image upload.");
            }
        }

        private async Task RemovePersonPhoto(People person)
        {
            if (string.IsNullOrEmpty(person.ImageUrl)) return;

            try
            {
                var fileName = person.ImageUrl.Split('/').LastOrDefault();
                if (!string.IsNullOrEmpty(fileName))
                    await _azureBlobUploader.DeleteFileAsync(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo.");
            }

            person.ImageUrl = null;
        }
    }
}
