using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace PC2.Controllers
{
    public class ResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TelemetryClient _telemetryClient;

        public ResourcesController(ApplicationDbContext context, TelemetryClient telemetryClient = null)
        {
            _context = context;
            _telemetryClient = telemetryClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Searches for agencies with the category the user selected from the table of categories.
        /// </summary>
        /// <param name="categoryID">The categoryID of the user's selection.</param>
        [HttpGet]
        public async Task<IActionResult> ResourceGuide(int categoryID)
        {
            ResourceGuideModel resourceGuide = new ResourceGuideModel();

            ViewData["ShowFeedbackForm"] = false;

            if (categoryID != 0)
            {
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, categoryID);
                resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, categoryID);
                TrackResourceGuideTelemetry("Manual/Category", resourceGuide.Category.AgencyCategoryName);

                ViewData["ShowFeedbackForm"] = true;
            }

            await AgencyDB.GetDataForDataLists(_context, resourceGuide);


            return View(resourceGuide);
        }

        /// <summary>
        /// Logs telemetry data for search events in the Resource Guide
        /// </summary>
        /// <param name="searchType">The type of search the user performed. This is a key in the telemetry system 
        /// and can be filtered and queried</param>
        /// <param name="searchTerm">The term used for searching (city, agency, etc)</param>
        private void TrackResourceGuideTelemetry(string searchType, string searchTerm)
        {
            _telemetryClient.TrackEvent("ResourceGuideSearch",
                        new Dictionary<string, string>{
                        { "SearchType", searchType },
                        { "SearchTerm", searchTerm }
            });
        }

        /// <summary>
        /// Searches for agencies that match the criteria the user searched for.
        /// </summary>
        /// <param name="searchModel">A model containing what the user searched for.</param>
        [HttpPost]
        public async Task<IActionResult> ResourceGuide(ResourceGuideModel searchModel)
        {
            ResourceGuideModel resourceGuide = new()
            {
                UserSearchedByCityOrService = searchModel.UserSearchedByCityOrService,
                UserSearchedByAgency = searchModel.UserSearchedByAgency
            };

            ViewBag.ShowFeedbackForm = false;

            if (!string.IsNullOrEmpty(searchModel.UserSearchedByAgency))
            {
                if (searchModel.SearchedAgency != null)
                {
                    TrackResourceGuideTelemetry("Agency", searchModel.SearchedAgency);
                    resourceGuide.Agencies = await AgencyDB.GetAgenciesByName(_context, searchModel.SearchedAgency);
                    ViewData["ShowFeedbackForm"] = true;
                }
            }
            else if (!string.IsNullOrEmpty(searchModel.UserSearchedByCityOrService))
            {
                if (searchModel.SearchedCategory != null
                && searchModel.SearchedCity != null)
                {
                    TrackResourceGuideTelemetry("CityAndCategory", $"{searchModel.SearchedCity} - {searchModel.SearchedCategory}");
                    resourceGuide.Agencies = await AgencyDB.GetAgenciesByCategoryAndCity(_context,
                        searchModel.SearchedCategory, searchModel.SearchedCity);
                    resourceGuide.CurrentCity = searchModel.SearchedCity;
                    resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, searchModel.SearchedCategory);
                    ViewData["ShowFeedbackForm"] = true;
                }
                else if (searchModel.SearchedCategory != null)
                {
                    TrackResourceGuideTelemetry("Service", $"{searchModel.SearchedCategory}");
                    resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, searchModel.SearchedCategory);
                    resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, resourceGuide.Category.AgencyCategoryId);
                    ViewData["ShowFeedbackForm"] = true;
                }
                else if (searchModel.SearchedCity != null)
                {
                    TrackResourceGuideTelemetry("City", searchModel.SearchedCity);
                    resourceGuide.CurrentCity = searchModel.SearchedCity;
                    resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, searchModel.SearchedCity);
                    ViewData["ShowFeedbackForm"] = true;
                }
            }
            
            await AgencyDB.GetDataForDataLists(_context, resourceGuide);
            return View(resourceGuide);
        }

        public async Task<IActionResult> Details(int id)
        {
            Agency agency = await AgencyDB.GetAgencyAsync(_context, id);
            return View(agency);
        }
            
        public IActionResult DisabilityAwareness()
        {
            return View();
        }

        public IActionResult ResourceLinks()
        {
            return View();
        }

        public IActionResult AgeSpecificIssues()
        {
            return View();
        }

        public IActionResult LegislativeLinksAndEvents()
        {
            return View();
        }

        public IActionResult EmergencyPreparedness()
        {
            return View();
        }

        public IActionResult VirtualCloset()
        {
            return View();
        }

        public async Task <IActionResult> FocusNewsletters()
        {
            List<NewsletterFile> newsletterFiles = await NewsletterFileDB.GetAllAsync(_context);

            return View(newsletterFiles);
        }

        /// <summary>
        /// Submits user feedback.
        /// </summary>
        /// <param name="model">The feedback model containing user input.</param>
        /// <returns>A redirect to the ResourceGuide action on success, 
        /// or the ResourceGuide view if the model is invalid.</returns>
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(Feedback model)
        {
            if (ModelState.IsValid)
            {
                model.SubmittedAt = DateTime.UtcNow;
                _context.Feedback.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for your feedback!";
                return RedirectToAction("ResourceGuide");
            }

            return RedirectToAction("ResourceGuide");
        }

        /// <summary>
        /// Allows administrators to view feedback submitted by users.
        /// This method retrieves and displays all feedback entries from the database, ordered by submission date.
        /// </summary>
        /// <returns>
        /// A view displaying the list of feedback, including their comments and submission dates.
        /// </returns>
        [Authorize(Roles = IdentityHelper.Admin)]
        [HttpGet]
        public async Task<IActionResult> ViewFeedback()
        {
            List<FeedbackViewModel> feedbackList = await _context.Feedback
                .OrderByDescending(f => f.SubmittedAt)
                .Select(f => new FeedbackViewModel
                {
                    Id = f.Id,
                    IsResourceFound = f.IsResourceFound ? "Yes" : "No",
                    Comments = f.Comments,
                    FormattedSubmittedAt = f.SubmittedAt.ToString("yyyy-MM-dd HH:mm"),
                    IsReviewed = f.IsReviewed
                })
                .ToListAsync();

            return View(feedbackList);
        }

        /// <summary>
        /// Allows administrators to view and edit feedback submitted by users.
        /// This method retrieves the feedback based on its ID and prepares it for editing.
        /// </summary>
        /// <param name="id">The ID of the feedback to edit.</param>
        /// <returns>
        /// A view displaying the feedback details, ready for editing, or a 404 error if the feedback does not exist.
        /// </returns>
        [Authorize(Roles = IdentityHelper.Admin)]
        [HttpGet]
        public async Task<IActionResult> EditFeedback(int id)
        {
            Feedback? feedback = await _context.Feedback.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            FeedbackViewModel viewModel = new()
            {
                Id = feedback.Id,
                IsResourceFound = feedback.IsResourceFound ? "Yes" : "No",
                Comments = feedback.Comments,
                FormattedSubmittedAt = feedback.SubmittedAt.ToString("yyyy-MM-dd HH:mm"),
                IsReviewed = feedback.IsReviewed
            };

            return View(viewModel);
        }

        /// <summary>
        /// Updates the feedback submitted by a user.
        /// This method processes the edits made by the administrator and updates the feedback in the database.
        /// </summary>
        /// <param name="model">The feedback view model containing the updated data.</param>
        /// <returns>
        /// A redirect to the "ViewFeedback" action if the feedback is successfully updated.
        /// A view with validation errors if the model is invalid.
        /// </returns>
        [Authorize(Roles = IdentityHelper.Admin)]
        [HttpPost]
        public async Task<IActionResult> EditFeedback(FeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Feedback? feedback = await _context.Feedback.FindAsync(model.Id);
            if (feedback == null)
            {
                return NotFound();
            }

            feedback.Comments = model.Comments;
            feedback.IsReviewed = model.IsReviewed;

            _context.Feedback.Update(feedback);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewFeedback));
        }

        /// <summary>
        /// Allows administrators to view the confirmation page for deleting feedback.
        /// This method retrieves the feedback based on its ID and presents a confirmation page to the user.
        /// </summary>
        /// <param name="id">The ID of the feedback to delete.</param>
        /// <returns>
        /// A view displaying the feedback to confirm deletion, or a 404 error if the feedback does not exist.
        /// </returns>
        [Authorize(Roles = IdentityHelper.Admin)]
        [HttpGet]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            Feedback? feedback = await _context.Feedback.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        /// <summary>
        /// Deletes the specified feedback from the database.
        /// This method removes the feedback entry from the database after confirmation.
        /// </summary>
        /// <param name="id">The ID of the feedback to delete.</param>
        /// <returns>
        /// A redirect to the "ViewFeedback" action after the feedback has been deleted.
        /// </returns>
        [Authorize(Roles = IdentityHelper.Admin)]
        [HttpPost, ActionName("DeleteFeedback")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Feedback? feedback = await _context.Feedback.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            _context.Feedback.Remove(feedback);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewFeedback));
        }

        /// <summary>
        /// Displays a view for marking a feedback entry as reviewed.
        /// </summary>
        /// <param name="id">The ID of the feedback entry to mark as reviewed.</param>
        /// <returns>The MarkAsReviewed view with the feedback data.</returns>
        [HttpGet]
        public async Task<IActionResult> MarkAsReviewed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Feedback? feedback = await _context.Feedback.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        /// <summary>
        /// Marks a feedback entry as reviewed.
        /// </summary>
        /// <param name="id">The ID of the feedback entry to mark as reviewed.</param>
        /// <returns>A redirect to the ViewFeedback action.</returns>
        [Authorize(Roles = IdentityHelper.Admin)]
        [HttpPost]
        public async Task<IActionResult> MarkAsReviewed(int id)
        {
            Feedback? feedback = await _context.Feedback.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            feedback.IsReviewed = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("ViewFeedback");
        }

    }
}
