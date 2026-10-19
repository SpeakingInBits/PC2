using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;
using PC2.Models.ViewModels;
using PC2.Services;

namespace PC2.Controllers;

[Authorize(Roles = IdentityHelper.AdminOrStaff)]
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

    public async Task<IActionResult> Index(PersonType? type = null, string? searchTerm = null)
    {
        IEnumerable<PersonViewModel> people = type switch
        {
            PersonType.Staff => (await StaffDB.GetAllStaffForEditing(_context)).Select(PersonViewModel.FromStaff),
            PersonType.Board => (await BoardDB.GetAllBoardMembersForEditing(_context)).Select(PersonViewModel.FromBoard),
            PersonType.SteeringCommittee => (await SteeringCommitteeDB.GetAllSteeringCommittee(_context)).Select(PersonViewModel.FromSteeringCommittee),
            null => (await StaffDB.GetAllStaffForEditing(_context)).Select(PersonViewModel.FromStaff)
                .Concat((await BoardDB.GetAllBoardMembersForEditing(_context)).Select(PersonViewModel.FromBoard))
                .Concat((await SteeringCommitteeDB.GetAllSteeringCommittee(_context)).Select(PersonViewModel.FromSteeringCommittee)),
            _ => []
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            people = people.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // Store filter values in ViewData for the view
        ViewData["SelectedType"] = type;
        ViewData["SearchTerm"] = searchTerm;

        return View(people.ToList());
    }

    [HttpGet]
    public IActionResult Create(PersonType? type = null)
    {
        // If no type is provided, default to Staff so the form displays
        var selectedType = type ?? PersonType.Staff;
        return View(new PersonViewModel { Type = selectedType });
    }

    [HttpPost]
    public async Task<IActionResult> Create(PersonViewModel model)
    {
        ValidateTypeSpecificFields(model);

        if (ModelState.IsValid)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating person record.");
                TempData["Message"] = "There was a problem creating this record. Please try again later.";
            }
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
            try
            {
                string? replacedImageUrl = null;
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
                        replacedImageUrl = await UpdatePhoto(model, staff);
                        await StaffDB.SaveChanges(_context, staff);
                        break;

                    case PersonType.Board:
                        var board = await BoardDB.GetBoardMember(_context, model.ID);
                        if (board == null) return NotFound();
                        board.Name = model.Name;
                        board.Title = model.Title;
                        board.MembershipStart = model.MembershipStart!;
                        board.PriorityOrder = model.PriorityOrder;
                        replacedImageUrl = await UpdatePhoto(model, board);
                        await BoardDB.EditBoardMember(_context, board);
                        break;

                    case PersonType.SteeringCommittee:
                        var sc = await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, model.ID);
                        if (sc == null) return NotFound();
                        sc.Name = model.Name;
                        sc.Title = model.Title;
                        sc.PriorityOrder = model.PriorityOrder;
                        replacedImageUrl = await UpdatePhoto(model, sc);
                        await SteeringCommitteeDB.EditSteeringCommittee(_context, sc);
                        break;
                }

                // Only deleted once the record is saved, so a failed upload or save keeps the current photo
                await DeleteStoredPhoto(replacedImageUrl);
                return RedirectToAction(nameof(Index), new { type = model.Type });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing person record.");
                TempData["Message"] = "There was a problem saving this record. Please try again later.";
            }
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
        try
        {
            // Photos are deleted after the record, so a failed delete keeps the record's photo
            switch (type)
            {
                case PersonType.Staff:
                    var staff = await StaffDB.GetStaffMember(_context, id);
                    if (staff == null) return NotFound();
                    await StaffDB.Delete(_context, staff);
                    await DeleteStoredPhoto(staff.ImageUrl);
                    break;

                case PersonType.Board:
                    var board = await BoardDB.GetBoardMember(_context, id);
                    if (board == null) return NotFound();
                    await BoardDB.Delete(_context, board);
                    await DeleteStoredPhoto(board.ImageUrl);
                    break;

                case PersonType.SteeringCommittee:
                    var sc = await SteeringCommitteeDB.GetSteeringCommitteeMember(_context, id);
                    if (sc == null) return NotFound();
                    await SteeringCommitteeDB.Delete(_context, sc);
                    await DeleteStoredPhoto(sc.ImageUrl);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting person record.");
            TempData["Message"] = "There was a problem deleting this record. Please try again later.";
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

    /// <summary>
    /// Applies the edit form's photo changes to the person: uploads the new photo, or clears the
    /// current one if it is being removed. The current photo stays in storage.
    /// </summary>
    /// <returns>The URL of the photo that was replaced or removed, to delete once the record is saved</returns>
    private async Task<string?> UpdatePhoto(PersonViewModel model, People person)
    {
        var currentImageUrl = person.ImageUrl;

        if (model.RemovePhoto) person.ImageUrl = null;
        else await HandlePhotoUpload(model.PhotoFile, person, model.ID);

        // A new photo stored under the same name overwrote the current one, so it must not be deleted
        return person.ImageUrl != currentImageUrl ? currentImageUrl : null;
    }

    private async Task HandlePhotoUpload(IFormFile? photoFile, People person, int? personId = null)
    {
        if (photoFile == null || photoFile.Length == 0) return;

        if (!ImageService.IsValidImageFile(photoFile))
            throw new InvalidOperationException("Please upload a valid image file (JPEG, PNG, GIF, or BMP).");

        // Name and serve the photo by the format ImageService actually wrote, which can differ from the upload
        using var resizedImage = await _imageService.ResizeImageAsync(photoFile.OpenReadStream(), 350, 350);
        var safeFileName = ImageService.GetSafeImageFileName(personId ?? 0, resizedImage.FileExtension);
        var resizedFormFile = new FormFileFromStream(resizedImage.Content, safeFileName, resizedImage.ContentType);
        person.ImageUrl = await _azureBlobUploader.UploadFileAsync(resizedFormFile, safeFileName, resizedImage.ContentType);
    }

    /// <summary>
    /// Deletes a photo from blob storage once no saved record points at it. A failure is only
    /// logged, because the record is already saved and the photo is just left unused.
    /// </summary>
    private async Task DeleteStoredPhoto(string? imageUrl)
    {
        var fileName = imageUrl?.Split('/').LastOrDefault();
        if (string.IsNullOrEmpty(fileName)) return;

        try
        {
            await _azureBlobUploader.DeleteFileAsync(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo {FileName}.", fileName);
        }
    }
}
