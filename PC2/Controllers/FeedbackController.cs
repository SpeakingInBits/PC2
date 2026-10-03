using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PC2.Data;
using PC2.Models;
using PC2.Services;

namespace PC2.Controllers
{
    /// <summary>
    /// Collects Resource Guide feedback from visitors and lets Admin and Staff review it
    /// </summary>
    [Authorize(Roles = IdentityHelper.AdminOrStaff)]
    public class FeedbackController : Controller
    {
        public const string ReCaptchaAction = "resource_guide_feedback";

        private readonly ApplicationDbContext _context;
        private readonly IReCaptchaService _reCaptchaService;
        private readonly FeedbackDigestService _digestService;
        private readonly FeedbackDigestOptions _digestOptions;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(ApplicationDbContext context, IReCaptchaService reCaptchaService,
            FeedbackDigestService digestService, IOptions<FeedbackDigestOptions> digestOptions,
            TimeProvider timeProvider, ILogger<FeedbackController> logger)
        {
            _context = context;
            _reCaptchaService = reCaptchaService;
            _digestService = digestService;
            _digestOptions = digestOptions.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        /// <summary>
        /// Saves feedback submitted from the Resource Guide. Called with fetch so the visitor keeps their search results.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(FeedbackSubmission submission, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                string message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                    ?? "Your feedback could not be submitted.";
                return BadRequest(new { message });
            }

            ReCaptchaVerificationResult reCaptchaResult =
                await _reCaptchaService.VerifyAsync(submission.ReCaptchaToken ?? "", ReCaptchaAction, cancellationToken);
            if (reCaptchaResult == ReCaptchaVerificationResult.Failed)
            {
                return BadRequest(new { message = "We couldn't verify your feedback. Please refresh the page and try again." });
            }
            if (reCaptchaResult == ReCaptchaVerificationResult.Unavailable)
            {
                // Don't lose genuine feedback when Google can't be reached; the reason is logged by the service
                _logger.LogWarning("Accepting Resource Guide feedback without reCAPTCHA verification.");
            }

            Feedback feedback = new()
            {
                IsResourceFound = submission.IsResourceFound!.Value,
                Comments = string.IsNullOrWhiteSpace(submission.Comments) ? null : submission.Comments.Trim(),
                SearchedFor = Truncate(submission.SearchedFor, Feedback.SearchedForMaxLength),
                SubmittedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            await FeedbackDB.AddAsync(_context, feedback);

            return Ok(new { message = "Thank you for your feedback!" });
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        /// <summary>
        /// Lists feedback for Admin and Staff. By default only shows feedback that has not been emailed or reviewed.
        /// </summary>
        /// <param name="showAll">True to include feedback that has already been emailed or reviewed</param>
        public async Task<IActionResult> Index(bool showAll = false)
        {
            List<Feedback> feedback = showAll
                ? await FeedbackDB.GetAllAsync(_context)
                : (await FeedbackDB.GetUnhandledAsync(_context)).OrderByDescending(f => f.SubmittedAt).ToList();

            ManageFeedbackViewModel viewModel = new()
            {
                Feedback = feedback,
                ShowAll = showAll,
                NewFeedbackCount = showAll ? await FeedbackDB.GetUnhandledCountAsync(_context) : feedback.Count,
                LastDigest = await FeedbackDB.GetLastDigestAsync(_context),
                DigestSchedule = _digestOptions.Enabled
                    ? $"{_digestOptions.SendDay} at {DateTime.Today.AddHours(_digestOptions.SendHour):h tt}"
                    : null,
                TimeZone = _digestOptions.GetTimeZoneInfo()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReviewed(int id, bool showAll = false)
        {
            if (!await FeedbackDB.MarkReviewedAsync(_context, id, _timeProvider.GetUtcNow().UtcDateTime))
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index), new { showAll });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllReviewed()
        {
            int count = await FeedbackDB.MarkAllUnhandledReviewedAsync(_context, _timeProvider.GetUtcNow().UtcDateTime);
            TempData["Message"] = count == 1 ? "1 feedback entry marked as reviewed" : $"{count} feedback entries marked as reviewed";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Sends the digest email now instead of waiting for the weekly schedule
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDigest(CancellationToken cancellationToken)
        {
            FeedbackDigestResult result = await _digestService.SendNowAsync(cancellationToken);
            switch (result.Status)
            {
                case FeedbackDigestStatus.Sent:
                    TempData["Message"] = $"Feedback email sent with {result.FeedbackCount} feedback " +
                                          (result.FeedbackCount == 1 ? "entry" : "entries");
                    break;
                case FeedbackDigestStatus.NothingToSend:
                    TempData["Message"] = "There is no new feedback to email";
                    break;
                default:
                    TempData["ErrorMessage"] = "The feedback email could not be sent. Please try again later.";
                    break;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            Feedback? feedback = await FeedbackDB.GetFeedbackAsync(_context, id);
            if (feedback == null)
            {
                return NotFound();
            }

            ViewData["TimeZone"] = _digestOptions.GetTimeZoneInfo();
            return View(feedback);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int feedbackId)
        {
            await FeedbackDB.DeleteAsync(_context, feedbackId);
            TempData["Message"] = "Feedback deleted";
            return RedirectToAction(nameof(Index));
        }
    }
}
