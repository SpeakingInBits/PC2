namespace PC2.Models
{
    /// <summary>
    /// View model for displaying Application Insights analytics data in the Admin dashboard
    /// </summary>
    public class AnalyticsViewModel
    {
        /// <summary>
        /// List of page views for the specified date range
        /// </summary>
        public List<PageViewData> PageViews { get; set; } = new List<PageViewData>();

        /// <summary>
        /// List of PDF download events
        /// </summary>
        public List<DownloadData> PdfDownloads { get; set; } = new List<DownloadData>();

        /// <summary>
        /// List of custom search terms from the resource guide
        /// </summary>
        public List<SearchTermData> SearchTerms { get; set; } = new List<SearchTermData>();

        /// <summary>
        /// Total number of page views in the specified date range
        /// </summary>
        public int TotalPageViews { get; set; }

        /// <summary>
        /// Total number of PDF downloads
        /// </summary>
        public int TotalPdfDownloads { get; set; }

        /// <summary>
        /// Total number of searches
        /// </summary>
        public int TotalSearches { get; set; }

        /// <summary>
        /// Start date for the analytics filter
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for the analytics filter
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Selected month for month/year filter (1-12)
        /// </summary>
        public int? SelectedMonth { get; set; }

        /// <summary>
        /// Selected year for month/year filter
        /// </summary>
        public int? SelectedYear { get; set; }
    }

    /// <summary>
    /// Represents page view data from Application Insights
    /// </summary>
    public class PageViewData
    {
        /// <summary>
        /// The page URL or path that was viewed
        /// </summary>
        public string PageUrl { get; set; } = string.Empty;

        /// <summary>
        /// The number of times this page was viewed
        /// </summary>
        public int ViewCount { get; set; }

        /// <summary>
        /// Timestamp of when the page was viewed (for detailed logs)
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Represents PDF download data from Application Insights
    /// </summary>
    public class DownloadData
    {
        /// <summary>
        /// The URL of the downloaded file
        /// </summary>
        public string FileUrl { get; set; } = string.Empty;

        /// <summary>
        /// The name of the downloaded file
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Number of times this file was downloaded
        /// </summary>
        public int DownloadCount { get; set; }

        /// <summary>
        /// Timestamp of the download
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Represents search term data from the Resource Guide
    /// </summary>
    public class SearchTermData
    {
        /// <summary>
        /// The type of search performed (e.g., "City", "Service", "Agency", etc.)
        /// </summary>
        public string SearchType { get; set; } = string.Empty;

        /// <summary>
        /// The actual search term used
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Number of times this search was performed
        /// </summary>
        public int SearchCount { get; set; }

        /// <summary>
        /// Timestamp of the search
        /// </summary>
        public DateTime? Timestamp { get; set; }
    }
}
