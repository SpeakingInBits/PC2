using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers;

[Authorize(Roles = IdentityHelper.AdminOrStaff)]
public class AboutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AzureBlobUploader _azureBlobUploader;
    private readonly ILogger<AboutController> _logger;

    public AboutController(ApplicationDbContext context, AzureBlobUploader azureBlobUploader, ILogger<AboutController> logger)
    {
        _context = context;
        _azureBlobUploader = azureBlobUploader;
        _logger = logger;
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
        {
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

        List<HousingProgram> data = await _context.HousingProgram.OrderBy(hp => hp.HouseHoldSize).ToListAsync();
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
            if (!PdfFileValidator.IsPdf(userFile))
            {
                TempData["Message"] = $"Error uploading file: {userFile.FileName} is not a PDF. Only PDF files can be uploaded.";
                return View(await NewsletterFileDB.GetAllAsync(_context));
            }

            try
            {
                string filePath = await _azureBlobUploader.UploadPdfAsync(userFile);
                TempData["Message"] = $"{userFile.FileName} uploaded successfully";

                NewsletterFile newsLetterFile = new()
                {
                    Name = userFile.FileName,
                    Location = filePath,
                };

                await NewsletterFileDB.AddAsync(_context, newsLetterFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading newsletter {FileName}", userFile.FileName);
                TempData["Message"] = $"Error uploading {userFile.FileName}. Please try again.";
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
            if (newsletter != null)
            {
                string? originalFile = newsletter.Location?.Split('/').LastOrDefault();
                if (originalFile != null)
                {
                    bool isDeleted = await _azureBlobUploader.DeleteFileAsync(originalFile);
                }

                await NewsletterFileDB.DeleteAsync(_context, newsletter.NewsletterId);
                TempData["Message"] = $"{newsletter.Name} deleted successfully";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting newsletter {NewsletterId}", id);
            TempData["Message"] = "Error deleting the file. Please try again.";
        }

        return RedirectToAction("UploadNewsletter");
    }

    [HttpGet]
    public async Task<IActionResult> RenameNewsletter(int id)
    {
        return View(await NewsletterFileDB.GetFileAsync(_context, id));
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
