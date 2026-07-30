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

        public JobsController(ApplicationDbContext context, AzureBlobUploader azureBlobUploader)
        {
            _context = context;
            _azureBlobUploader = azureBlobUploader;
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

            if (attachmentFile != null)
            {
                try
                {
                    string filePath = await _azureBlobUploader.UploadFileAsync(attachmentFile, attachmentFile.FileName);
                    job.AttachmentLocation = filePath;
                    job.AttachmentName = attachmentFile.FileName;
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Error uploading attachment: {ex.Message}";
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

            if (!ModelState.IsValid)
            {
                return View(job);
            }

            if (attachmentFile != null)
            {
                try
                {
                    // Remove the old attachment from blob storage when it is being replaced
                    string? oldBlobName = job.AttachmentLocation?.Split('/').LastOrDefault();
                    if (oldBlobName != null && oldBlobName != attachmentFile.FileName)
                    {
                        await _azureBlobUploader.DeleteFileAsync(oldBlobName);
                    }

                    string filePath = await _azureBlobUploader.UploadFileAsync(attachmentFile, attachmentFile.FileName);
                    job.AttachmentLocation = filePath;
                    job.AttachmentName = attachmentFile.FileName;
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Error uploading attachment: {ex.Message}";
                    return View(job);
                }
            }

            await JobOpportunityDB.UpdateAsync(_context, job);
            TempData["Message"] = $"Job \"{job.Title}\" updated successfully";
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
                TempData["Message"] = $"Error deleting job: {ex.Message}";
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
