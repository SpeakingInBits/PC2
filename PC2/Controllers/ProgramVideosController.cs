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

            if (ModelState.IsValid)
            {
                if (pdfFile != null)
                {
                    try
                    {
                        string filePath = await _azureBlobUploader.UploadFileAsync(pdfFile, pdfFile.FileName);
                        programVideo.PdfLocation = filePath;
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

            if (ModelState.IsValid)
            {
                if (pdfFile != null)
                {
                    try
                    {
                        string filePath = await _azureBlobUploader.UploadFileAsync(pdfFile, pdfFile.FileName);
                        programVideo.PdfLocation = filePath;
                        programVideo.PdfName = pdfFile.FileName;
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = $"Error uploading PDF: {ex.Message}";
                        return View(programVideo);
                    }
                }

                await ProgramVideoDB.UpdateAsync(_context, programVideo);
                TempData["Message"] = $"Video \"{programVideo.Title}\" updated successfully";
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
