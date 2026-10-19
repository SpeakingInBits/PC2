using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.AdminOrStaff)]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobUploader _azureBlobUploader;
        private readonly ILogger<JobsController> _logger;

        public JobsController(ApplicationDbContext context, AzureBlobUploader azureBlobUploader, ILogger<JobsController> logger)
        {
            _context = context;
            _azureBlobUploader = azureBlobUploader;
            _logger = logger;
        }

        /// <summary>
        /// Public listing of open job opportunities
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            List<JobOpportunity> jobs = await JobOpportunityDB.GetOpenAsync(_context);

            List<JobOpportunityViewModel> viewModels = jobs.Select(j => new JobOpportunityViewModel
            {
                Job = j,
                SanitizedDescription = TextLinkifier.Linkify(j.Description)
            }).ToList();

            return View(viewModels);
        }

        /// <summary>
        /// Admin listing of all job opportunities with open jobs first
        /// </summary>
        public async Task<IActionResult> Manage()
        {
            List<JobOpportunity> jobs = await JobOpportunityDB.GetAllAsync(_context);
            return View(jobs);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(JobOpportunity job, IFormFile? attachmentFile)
        {
            // Remove attachment fields from model validation since they are optional and set by the upload
            ModelState.Remove(nameof(JobOpportunity.AttachmentLocation));
            ModelState.Remove(nameof(JobOpportunity.AttachmentName));

            if (!ModelState.IsValid)
            {
                return View(job);
            }

            // The attachment is only ever set by an upload, never by the form
            job.AttachmentLocation = null;
            job.AttachmentName = null;

            if (attachmentFile != null)
            {
                if (!PdfFileValidator.IsPdf(attachmentFile))
                {
                    TempData["Message"] = $"Error uploading attachment: {attachmentFile.FileName} is not a PDF. Only PDF files can be uploaded.";
                    return View(job);
                }

                try
                {
                    job.AttachmentLocation = await _azureBlobUploader.UploadPdfAsync(attachmentFile);
                    job.AttachmentName = attachmentFile.FileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading attachment {FileName} for new job {Title}", attachmentFile.FileName, job.Title);
                    TempData["Message"] = $"Error uploading {attachmentFile.FileName}. Please try again.";
                    return View(job);
                }
            }

            await JobOpportunityDB.AddAsync(_context, job);
            TempData["Message"] = $"Job \"{job.Title}\" added successfully";
            return RedirectToAction("Manage");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            JobOpportunity? job = await JobOpportunityDB.GetJobAsync(_context, id);
            if (job == null)
            {
                return NotFound();
            }
            return View(job);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(JobOpportunity job, IFormFile? attachmentFile)
        {
            ModelState.Remove(nameof(JobOpportunity.AttachmentLocation));
            ModelState.Remove(nameof(JobOpportunity.AttachmentName));

            JobOpportunity? existing = await JobOpportunityDB.GetJobAsync(_context, job.JobOpportunityId);
            if (existing == null)
            {
                return NotFound();
            }

            // The attachment and closed state come from the database, never the form.
            // They're also shown again if the form is redisplayed.
            job.AttachmentLocation = existing.AttachmentLocation;
            job.AttachmentName = existing.AttachmentName;
            job.IsClosed = existing.IsClosed;

            if (!ModelState.IsValid)
            {
                return View(job);
            }

            string? oldBlobName = null;
            if (attachmentFile != null)
            {
                if (!PdfFileValidator.IsPdf(attachmentFile))
                {
                    TempData["Message"] = $"Error uploading attachment: {attachmentFile.FileName} is not a PDF. Only PDF files can be uploaded.";
                    return View(job);
                }

                try
                {
                    string filePath = await _azureBlobUploader.UploadPdfAsync(attachmentFile);

                    // The old attachment is removed from blob storage once the job is saved
                    oldBlobName = existing.AttachmentLocation?.Split('/').LastOrDefault();
                    existing.AttachmentLocation = filePath;
                    existing.AttachmentName = attachmentFile.FileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading attachment {FileName} for job {JobOpportunityId}", attachmentFile.FileName, existing.JobOpportunityId);
                    TempData["Message"] = $"Error uploading {attachmentFile.FileName}. Please try again.";
                    return View(job);
                }
            }

            existing.Title = job.Title;
            existing.Description = job.Description;
            existing.ClosingDate = job.ClosingDate;

            await JobOpportunityDB.UpdateAsync(_context, existing);
            TempData["Message"] = $"Job \"{existing.Title}\" updated successfully";

            if (oldBlobName != null)
            {
                try
                {
                    await _azureBlobUploader.DeleteFileAsync(oldBlobName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing previous attachment {BlobName} for job {JobOpportunityId}", oldBlobName, existing.JobOpportunityId);
                    TempData["Message"] = $"The job \"{existing.Title}\" was updated, but its previous attachment couldn't be removed from storage.";
                }
            }

            return RedirectToAction("Manage");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            JobOpportunity? job = await JobOpportunityDB.GetJobAsync(_context, id);
            if (job == null)
            {
                return NotFound();
            }
            return View(job);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            try
            {
                JobOpportunity? job = await JobOpportunityDB.GetJobAsync(_context, id);
                if (job != null)
                {
                    if (!string.IsNullOrEmpty(job.AttachmentLocation))
                    {
                        string? blobName = job.AttachmentLocation.Split('/').LastOrDefault();
                        if (blobName != null)
                        {
                            await _azureBlobUploader.DeleteFileAsync(blobName);
                        }
                    }

                    await JobOpportunityDB.DeleteAsync(_context, id);
                    TempData["Message"] = $"Job \"{job.Title}\" deleted successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobOpportunityId}", id);
                TempData["Message"] = "Error deleting the job. Please try again.";
            }

            return RedirectToAction("Manage");
        }

        /// <summary>
        /// Closes a job so it no longer appears on the public listing
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Close(int id)
        {
            JobOpportunity? job = await JobOpportunityDB.GetJobAsync(_context, id);
            if (job == null)
            {
                return NotFound();
            }

            job.IsClosed = true;
            await JobOpportunityDB.UpdateAsync(_context, job);
            TempData["Message"] = $"Job \"{job.Title}\" closed";
            return RedirectToAction("Manage");
        }

        /// <summary>
        /// Reopens a closed job. A closing date that has already passed is
        /// cleared so the job appears on the public listing again.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Reopen(int id)
        {
            JobOpportunity? job = await JobOpportunityDB.GetJobAsync(_context, id);
            if (job == null)
            {
                return NotFound();
            }

            job.IsClosed = false;
            if (job.ClosingDate != null && job.ClosingDate < DateOnly.FromDateTime(DateTime.Today))
            {
                job.ClosingDate = null;
            }

            await JobOpportunityDB.UpdateAsync(_context, job);
            TempData["Message"] = $"Job \"{job.Title}\" reopened";
            return RedirectToAction("Manage");
        }
    }
}
