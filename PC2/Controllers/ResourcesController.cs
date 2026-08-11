using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

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
            if (categoryID != 0)
            {
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, categoryID);
                resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, categoryID);
                TrackResourceGuideTelemetry("Manual/Category", 
                    resourceGuide.Category.AgencyCategoryName);
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

            if (!string.IsNullOrEmpty(searchModel.UserSearchedByAgency))
            {
                if (searchModel.SearchedAgency != null)
                {
                    TrackResourceGuideTelemetry("Agency", searchModel.SearchedAgency);
                    resourceGuide.Agencies = await AgencyDB.GetAgenciesByName(_context, searchModel.SearchedAgency);
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
                }
                else if (searchModel.SearchedCategory != null)
                {
                    TrackResourceGuideTelemetry("Service", $"{searchModel.SearchedCategory}");
                    resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, searchModel.SearchedCategory);
                    resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, resourceGuide.Category.AgencyCategoryId);
                }
                else if (searchModel.SearchedCity != null)
                {
                    TrackResourceGuideTelemetry("City", searchModel.SearchedCity);
                    resourceGuide.CurrentCity = searchModel.SearchedCity;
                    resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, searchModel.SearchedCity);
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

        public async Task<IActionResult> ResourceLinks()
        {
            var resourceLinks = await ResourceLinksDB.GetAllResourceLinks(_context);
            return View(resourceLinks);
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
        /// Tracks newsletter download events via AJAX
        /// </summary>
        /// <param name="linkUrl">The URL of the newsletter being downloaded</param>
        /// <param name="searchType">The type of action (e.g., "Download")</param>
        [HttpPost]
        public IActionResult TrackNewsletterDownload([FromBody] NewsletterDownloadRequest request)
        {
            if (_telemetryClient != null && !string.IsNullOrEmpty(request?.LinkUrl))
            {
                _telemetryClient.TrackEvent("FocusNewsletter",
                    new Dictionary<string, string>
                    {
                        { "linkUrl", request.LinkUrl },
                        { "SearchType", request.SearchType ?? "Download" }
                    });
            }

            return Ok();
        }

        #region Resource Links Management

        /// <summary>
        /// Displays admin management page listing all resource links (admin/staff only).
        /// </summary>
        [Authorize(Roles = IdentityHelper.AdminOrStaff)]
        public async Task<IActionResult> ManageResourceLinks()
        {
            var resourceLinks = await ResourceLinksDB.GetAllResourceLinks(_context);
            return View(resourceLinks);
        }

        /// <summary>
        /// Displays an empty form for creating a new resource link (admin/staff only).
        /// </summary>
        /// <returns>
        /// A view displaying the CreateResourceLink form with empty input fields.
        /// The view name is "CreateResourceLink".
        /// </returns>
        /// <remarks>
        /// HTTP Method: GET
        /// Authorization: Only users with Admin or Staff role can access this action.
        /// 
        /// Form Contents:
        /// - LinkText (required): Display name for the resource
        /// - LinkURL (required): URL or path to resource (external https:// or internal ~/)
        /// - Description (optional): Additional information about the resource
        /// 
        /// Form Submission:
        /// When user fills out and submits the form, POST Create(ResourceLinksModel) is called
        /// to validate and save the new resource.
        /// 
        /// Note: Alphabetical sorting/grouping is derived from
        /// LinkText (no separate FirstLetter column is stored).
        /// </remarks>
        [Authorize(Roles = IdentityHelper.AdminOrStaff)]
        [HttpPost]
        public IActionResult Create(ResourceLinksModel model)
        {
            if (!model.IsValidUrl())
            {
                ModelState.AddModelError(nameof(model.LinkURL),
                    "URL must start with http://, https://, or ~/");
                return View(model);
            }

            _context.ResourceLinks.Add(model);
            _context.SaveChanges();

            return RedirectToAction("ManageResourceLinks");
        }

        /// <summary>
        /// Handles POST to create new resource. Saves if valid, redisplays form if validation fails.
        /// </summary>
        /// <param name="resourceLink">Form data from CreateResourceLink view.</param>
        /// <returns>Redirect to ManageResourceLinks on success; form view on validation error.</returns>
        [Authorize(Roles = IdentityHelper.AdminOrStaff)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceLinksModel resourceLink)
        {
            if (ModelState.IsValid)
            {
                // Add to database
                await ResourceLinksDB.AddResourceLink(_context, resourceLink);
                // Redirect to management page to show the newly added resource
                return RedirectToAction(nameof(ManageResourceLinks));
            }

            // If validation failed, redisplay the form with error messages
            return View("CreateResourceLink", resourceLink);
        }

        /// <summary>
        /// Displays Edit form pre-populated with existing resource data (admin/staff only).
        /// </summary>
        /// <param name="id">The ResourceID to edit.</param>
        /// <returns>Edit form view if found; 404 if not found.</returns>
        [Authorize(Roles = IdentityHelper.AdminOrStaff)]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Retrieve the resource from database
            var resourceLink = await ResourceLinksDB.GetResourceLink(_context, id);

            // Return 404 if resource not found or ID is invalid
            if (resourceLink == null)
            {
                return NotFound();
            }

            // Display form pre-populated with current resource data
            return View("EditResourceLink", resourceLink);
        }

        /// <summary>
        /// Handles POST to update resource. Validates ID match, saves if valid.
        /// </summary>
        /// <param name="id">URL ID parameter.</param>
        /// <param name="resourceLink">Form data with updated values.</param>
        /// <returns>Redirect to ManageResourceLinks on success; form on error; 400 if ID mismatch.</returns>
        [Authorize(Roles = IdentityHelper.AdminOrStaff)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResourceLinksModel resourceLink)
        {
            // Security validation: URL ID must match form's ResourceID
            // Prevents tampering where URL points to one resource but form is for another
            if (id != resourceLink.ResourceID)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the resource in database with auto-recalculated FirstLetter
                    await ResourceLinksDB.UpdateResourceLink(_context, resourceLink);
                    // Redirect to management page to show updated list
                    return RedirectToAction(nameof(ManageResourceLinks));
                }
                catch (Exception)
                {
                    // Generic error handling for unexpected database issues
                    return StatusCode(500, "An error occurred while updating the resource link.");
                }
            }

            // If validation failed, redisplay the form with error messages
            return View("EditResourceLink", resourceLink);
        }

        /// <summary>
        /// Displays confirmation page before deleting resource (admin/staff only).
        /// </summary>
        /// <param name="id">The ResourceID to delete.</param>
        /// <returns>Confirmation view if found; 404 if not found.</returns>
        [Authorize(Roles = IdentityHelper.AdminOrStaff)]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // Retrieve the resource to show its details in confirmation form
            var resourceLink = await ResourceLinksDB.GetResourceLink(_context, id);

            // Return 404 if resource not found
            if (resourceLink == null)
            {
                return NotFound();
            }

            // Display confirmation view with resource details
            return View("DeleteResourceLink", resourceLink);
        }

        /// <summary>
        /// Handles POST to permanently delete resource. Calls DeleteResourceLinkById.
        /// </summary>
        /// <param name="id">The ResourceID to delete.</param>
        /// <param name="resourceLink">Form model (unused, for model binding).</param>
        /// <returns>Redirect to ManageResourceLinks on success or error.</returns>
        [Authorize(Roles = IdentityHelper.AdminOrStaff)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, ResourceLinksModel resourceLink)
        {
            try
            {
                // Delete the resource by ID (null-safe operation)
                await ResourceLinksDB.DeleteResourceLinkById(_context, id);
                // Redirect to management page to show updated list without deleted resource
                return RedirectToAction(nameof(ManageResourceLinks));
            }
            catch (Exception)
            {
                // Generic error handling for unexpected database issues
                return StatusCode(500, "An error occurred while deleting the resource link.");
            }
        }

        #endregion
    }

    /// <summary>
    /// Request model for newsletter download tracking
    /// </summary>
    public class NewsletterDownloadRequest
    {
        public string LinkUrl { get; set; }
        public string SearchType { get; set; }
    }
}
