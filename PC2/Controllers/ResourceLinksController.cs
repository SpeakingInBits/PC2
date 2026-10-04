using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    /// <summary>
    /// Lets Admin and Staff manage the links shown on the Resources/ResourceLinks page
    /// </summary>
    [Authorize(Roles = IdentityHelper.AdminOrStaff)]
    public class ResourceLinksController : Controller
    {
        private const string InvalidUrlMessage = "Enter a full website address starting with https://, or upload a PDF.";

        private readonly ApplicationDbContext _context;
        private readonly AzureBlobUploader _azureBlobUploader;

        public ResourceLinksController(ApplicationDbContext context, AzureBlobUploader azureBlobUploader)
        {
            _context = context;
            _azureBlobUploader = azureBlobUploader;
        }

        public async Task<IActionResult> Manage()
        {
            List<ResourceLink> links = await ResourceLinkDB.GetAllAsync(_context);
            return View(links);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ResourceLink resourceLink, IFormFile? uploadedFile)
        {
            // Url and FileName are set by the upload when a file is chosen
            ModelState.Remove(nameof(ResourceLink.Url));
            ModelState.Remove(nameof(ResourceLink.FileName));

            if (uploadedFile == null && !ResourceLink.IsValidUrl(resourceLink.Url))
            {
                ModelState.AddModelError(nameof(ResourceLink.Url), InvalidUrlMessage);
            }

            if (!ModelState.IsValid)
            {
                return View(resourceLink);
            }

            if (uploadedFile != null)
            {
                if (!PdfFileValidator.IsPdf(uploadedFile))
                {
                    TempData["Message"] = $"Error uploading file: {uploadedFile.FileName} is not a PDF. Only PDF files can be uploaded.";
                    return View(resourceLink);
                }

                try
                {
                    resourceLink.Url = await _azureBlobUploader.UploadFileAsync(uploadedFile, uploadedFile.FileName);
                    resourceLink.FileName = uploadedFile.FileName;
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Error uploading file: {ex.Message}";
                    return View(resourceLink);
                }
            }
            else
            {
                resourceLink.Url = resourceLink.Url.Trim();
                resourceLink.FileName = null;
            }

            await ResourceLinkDB.AddAsync(_context, resourceLink);
            TempData["Message"] = $"Link \"{resourceLink.Name}\" added successfully";
            return RedirectToAction("Manage");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ResourceLink? resourceLink = await ResourceLinkDB.GetLinkAsync(_context, id);
            if (resourceLink == null)
            {
                return NotFound();
            }
            return View(resourceLink);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ResourceLink resourceLink, IFormFile? uploadedFile)
        {
            ModelState.Remove(nameof(ResourceLink.Url));
            ModelState.Remove(nameof(ResourceLink.FileName));

            ResourceLink? existing = await ResourceLinkDB.GetLinkAsync(_context, resourceLink.ResourceLinkId);
            if (existing == null)
            {
                return NotFound();
            }

            // Shown again if the form is redisplayed
            resourceLink.FileName = existing.FileName;

            bool keepCurrentFile = uploadedFile == null && existing.FileName != null && resourceLink.Url == existing.Url;
            if (uploadedFile == null && !keepCurrentFile && !ResourceLink.IsValidUrl(resourceLink.Url))
            {
                ModelState.AddModelError(nameof(ResourceLink.Url), InvalidUrlMessage);
            }

            if (!ModelState.IsValid)
            {
                return View(resourceLink);
            }

            if (uploadedFile != null && !PdfFileValidator.IsPdf(uploadedFile))
            {
                TempData["Message"] = $"Error uploading file: {uploadedFile.FileName} is not a PDF. Only PDF files can be uploaded.";
                return View(resourceLink);
            }

            // The current file is removed from blob storage when it is replaced by a new file or a website address
            string? oldBlobName = existing.FileName != null && !keepCurrentFile ? existing.Url.Split('/').Last() : null;

            if (uploadedFile != null)
            {
                try
                {
                    existing.Url = await _azureBlobUploader.UploadFileAsync(uploadedFile, uploadedFile.FileName);
                    existing.FileName = uploadedFile.FileName;
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Error uploading file: {ex.Message}";
                    return View(resourceLink);
                }
            }
            else if (!keepCurrentFile)
            {
                existing.Url = resourceLink.Url.Trim();
                existing.FileName = null;
            }

            existing.Name = resourceLink.Name;
            existing.Description = resourceLink.Description;

            await ResourceLinkDB.UpdateAsync(_context, existing);
            TempData["Message"] = $"Link \"{existing.Name}\" updated successfully";

            // A new file with the same name overwrote the old one, so there is nothing to remove
            if (oldBlobName != null && oldBlobName != existing.Url.Split('/').Last())
            {
                try
                {
                    await _azureBlobUploader.DeleteFileAsync(oldBlobName);
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Error removing the previous file: {ex.Message}. The link \"{existing.Name}\" was updated.";
                }
            }

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ResourceLink? resourceLink = await ResourceLinkDB.GetLinkAsync(_context, id);
            if (resourceLink == null)
            {
                return NotFound();
            }
            return View(resourceLink);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            try
            {
                ResourceLink? resourceLink = await ResourceLinkDB.GetLinkAsync(_context, id);
                if (resourceLink != null)
                {
                    if (resourceLink.FileName != null)
                    {
                        string blobName = resourceLink.Url.Split('/').Last();
                        await _azureBlobUploader.DeleteFileAsync(blobName);
                    }

                    await ResourceLinkDB.DeleteAsync(_context, id);
                    TempData["Message"] = $"Link \"{resourceLink.Name}\" deleted successfully";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting link: {ex.Message}";
            }

            return RedirectToAction("Manage");
        }
    }
}
