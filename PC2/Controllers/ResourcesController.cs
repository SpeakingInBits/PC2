using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
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
            // The category may have been deleted since the link was saved
            AgencyCategory? category = categoryID != 0 ? await AgencyCategoryDB.GetAgencyCategory(_context, categoryID) : null;
            if (category != null)
            {
                resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, categoryID);
                resourceGuide.Category = category;
                TrackResourceGuideTelemetry("Manual/Category", resourceGuide.Category.AgencyCategoryName);
                resourceGuide.SearchDescription = $"Service: {resourceGuide.Category.AgencyCategoryName}";
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
                    resourceGuide.SearchDescription = $"Agency: {searchModel.SearchedAgency}";
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
                    resourceGuide.SearchDescription = $"Service: {searchModel.SearchedCategory}, City: {searchModel.SearchedCity}";
                }
                else if (searchModel.SearchedCategory != null)
                {
                    TrackResourceGuideTelemetry("Service", $"{searchModel.SearchedCategory}");
                    resourceGuide.Category = await AgencyCategoryDB.GetAgencyCategory(_context, searchModel.SearchedCategory);
                    // Typed names that don't match a category find no agencies
                    if (resourceGuide.Category != null)
                    {
                        resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, resourceGuide.Category.AgencyCategoryId);
                    }
                    resourceGuide.SearchDescription = $"Service: {searchModel.SearchedCategory}";
                }
                else if (searchModel.SearchedCity != null)
                {
                    TrackResourceGuideTelemetry("City", searchModel.SearchedCity);
                    resourceGuide.CurrentCity = searchModel.SearchedCity;
                    resourceGuide.Agencies = await AgencyDB.GetSpecificAgenciesAsync(_context, searchModel.SearchedCity);
                    resourceGuide.SearchDescription = $"City: {searchModel.SearchedCity}";
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
            List<ResourceLink> links = await ResourceLinkDB.GetAllAsync(_context);
            return View(links);
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

        public IActionResult EquipmentExchange()
        {
            return View();
        }

        /// <summary>
        /// The Virtual Closet was renamed to Emma's Exceptional Equipment Exchange; keeps old links and bookmarks working
        /// </summary>
        public IActionResult VirtualCloset()
        {
            return RedirectToActionPermanent(nameof(EquipmentExchange));
        }

        public async Task <IActionResult> FocusNewsletters()
        {
            List<NewsletterFile> newsletterFiles = await NewsletterFileDB.GetAllAsync(_context);

            return View(newsletterFiles);
        }

        /// <summary>
        /// Shows a Focus newsletter in the browser's PDF viewer, or downloads it.
        /// The stored blobs have no PDF content type, so linking to them directly always downloads.
        /// </summary>
        /// <param name="id">The NewsletterId of the newsletter</param>
        /// <param name="download">True to download the file instead of previewing it</param>
        [HttpGet]
        public async Task<IActionResult> Newsletter(int id, bool download, [FromServices] AzureBlobUploader azureBlobUploader)
        {
            NewsletterFile? newsletter = await NewsletterFileDB.GetFileAsync(_context, id);
            if (newsletter == null)
            {
                return NotFound();
            }

            Stream? pdf = await azureBlobUploader.OpenReadAsync(newsletter.Location);
            if (pdf == null)
            {
                return NotFound();
            }

            // Admins can rename newsletters, so the name may not end in .pdf
            string fileName = newsletter.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? newsletter.Name
                : newsletter.Name + ".pdf";

            if (download)
            {
                return File(pdf, "application/pdf", fileName);
            }

            // Inline shows the PDF in the browser; the file name is still used if the reader saves it
            ContentDispositionHeaderValue disposition = new("inline");
            disposition.SetHttpFileName(fileName);
            Response.Headers.ContentDisposition = disposition.ToString();
            return File(pdf, "application/pdf");
        }

        /// <summary>
        /// Tracks newsletter download events via AJAX
        /// </summary>
        /// <param name="linkUrl">The URL of the newsletter being downloaded</param>
        /// <param name="searchType">The type of action ("Preview" or "Download")</param>
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
