using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.AdminOrStaff)]
    public class ProgramVideosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobUploader _azureBlobUploader;

        public ProgramVideosController(ApplicationDbContext context, AzureBlobUploader azureBlobUploader)
        {
            _context = context;
            _azureBlobUploader = azureBlobUploader;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            List<ProgramVideo> videos = await ProgramVideoDB.GetAllAsync(_context);
            return View(videos);
        }

        public async Task<IActionResult> ManageVideos()
        {
            List<ProgramVideo> videos = await ProgramVideoDB.GetAllAsync(_context);
            return View(videos);
        }

        [HttpGet]
        public IActionResult CreateVideo()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateVideo(ProgramVideo programVideo, IFormFile? pdfFile)
        {
            // Remove PDF-related fields from model validation since they are optional and set by the upload
            ModelState.Remove(nameof(ProgramVideo.PdfLocation));
            ModelState.Remove(nameof(ProgramVideo.PdfName));

            // The PDF is only ever set by an upload, never by the form
            programVideo.PdfLocation = null;
            programVideo.PdfName = null;

            if (ModelState.IsValid)
            {
                if (pdfFile != null)
                {
                    if (!PdfFileValidator.IsPdf(pdfFile))
                    {
                        TempData["Message"] = $"Error uploading PDF: {pdfFile.FileName} is not a PDF. Only PDF files can be uploaded.";
                        return View(programVideo);
                    }

                    try
                    {
                        programVideo.PdfLocation = await _azureBlobUploader.UploadPdfAsync(pdfFile);
                        programVideo.PdfName = pdfFile.FileName;
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = $"Error uploading PDF: {ex.Message}";
                        return View(programVideo);
                    }
                }

                await ProgramVideoDB.AddAsync(_context, programVideo);
                TempData["Message"] = $"Video \"{programVideo.Title}\" added successfully";
                return RedirectToAction("ManageVideos");
            }

            return View(programVideo);
        }

        [HttpGet]
        public async Task<IActionResult> EditVideo(int id)
        {
            ProgramVideo? video = await ProgramVideoDB.GetVideoAsync(_context, id);
            if (video == null)
            {
                return NotFound();
            }
            return View(video);
        }

        [HttpPost]
        public async Task<IActionResult> EditVideo(ProgramVideo programVideo, IFormFile? pdfFile)
        {
            ModelState.Remove(nameof(ProgramVideo.PdfLocation));
            ModelState.Remove(nameof(ProgramVideo.PdfName));

            ProgramVideo? existing = await ProgramVideoDB.GetVideoAsync(_context, programVideo.ProgramVideoId);
            if (existing == null)
            {
                return NotFound();
            }

            // The PDF comes from the database, never the form. It's also shown again if the form is redisplayed.
            programVideo.PdfLocation = existing.PdfLocation;
            programVideo.PdfName = existing.PdfName;

            if (ModelState.IsValid)
            {
                string? oldBlobName = null;
                if (pdfFile != null)
                {
                    if (!PdfFileValidator.IsPdf(pdfFile))
                    {
                        TempData["Message"] = $"Error uploading PDF: {pdfFile.FileName} is not a PDF. Only PDF files can be uploaded.";
                        return View(programVideo);
                    }

                    try
                    {
                        string filePath = await _azureBlobUploader.UploadPdfAsync(pdfFile);

                        // The old PDF is removed from blob storage once the video is saved
                        oldBlobName = existing.PdfLocation?.Split('/').LastOrDefault();
                        existing.PdfLocation = filePath;
                        existing.PdfName = pdfFile.FileName;
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = $"Error uploading PDF: {ex.Message}";
                        return View(programVideo);
                    }
                }

                existing.Title = programVideo.Title;
                existing.YouTubeVideoId = programVideo.YouTubeVideoId;

                await ProgramVideoDB.UpdateAsync(_context, existing);
                TempData["Message"] = $"Video \"{existing.Title}\" updated successfully";

                if (oldBlobName != null)
                {
                    try
                    {
                        await _azureBlobUploader.DeleteFileAsync(oldBlobName);
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = $"Error removing the previous PDF: {ex.Message}. The video \"{existing.Title}\" was updated.";
                    }
                }

                return RedirectToAction("ManageVideos");
            }

            return View(programVideo);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            ProgramVideo? video = await ProgramVideoDB.GetVideoAsync(_context, id);
            if (video == null)
            {
                return NotFound();
            }
            return View(video);
        }

        [HttpPost]
        [ActionName("DeleteVideo")]
        public async Task<IActionResult> ConfirmDeleteVideo(int id)
        {
            try
            {
                ProgramVideo? video = await ProgramVideoDB.GetVideoAsync(_context, id);
                if (video != null)
                {
                    if (!string.IsNullOrEmpty(video.PdfLocation))
                    {
                        string? blobName = video.PdfLocation.Split('/').LastOrDefault();
                        if (blobName != null)
                        {
                            await _azureBlobUploader.DeleteFileAsync(blobName);
                        }
                    }

                    await ProgramVideoDB.DeleteAsync(_context, id);
                    TempData["Message"] = $"Video \"{video.Title}\" deleted successfully";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting video: {ex.Message}";
            }

            return RedirectToAction("ManageVideos");
        }
    }
}
