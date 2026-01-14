using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure;
using PC2.Models;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace PC2.Services;

/// <summary>
/// Service for querying Application Insights telemetry data from Azure or local development
/// </summary>
public class AnalyticsService
{
    private readonly LogsQueryClient? _logsQueryClient;
    private readonly string? _workspaceId;
    private readonly bool _isAzureConfigured;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IConfiguration configuration, ILogger<AnalyticsService> logger)
    {
        _logger = logger;
        _workspaceId = configuration["ApplicationInsights:WorkspaceId"];
        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

#if !DEBUG
        try
        {
            _logsQueryClient = new LogsQueryClient(new ManagedIdentityCredential());
            _isAzureConfigured = true;
            _logger.LogInformation("Successfully initialized Azure Application Insights client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Application Insights client");
            _logger.LogWarning("Falling back to local Application Insights data");
            _isAzureConfigured = false;
        }
#else
        // Use local hardcoded data for dev
        _isAzureConfigured = false;
#endif
    }

    /// <summary>
    /// Retrieves analytics data for the specified date range including page views, downloads, and search terms
    /// </summary>
    /// <param name="startDate">Start date for the query (defaults to 30 days ago)</param>
    /// <param name="endDate">End date for the query (defaults to now)</param>
    /// <returns>AnalyticsViewModel populated with data from Application Insights</returns>
    public async Task<AnalyticsViewModel> GetAnalyticsDataAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var viewModel = new AnalyticsViewModel
        {
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
            EndDate = endDate ?? DateTime.UtcNow
        };

        try
        {
            if (_isAzureConfigured && _logsQueryClient != null)
            {
                // Use Azure Application Insights Log Analytics
                await GetAzureAnalyticsDataAsync(viewModel);
            }
            else
            {
                // Use local Application Insights telemetry (development mode)
                GetLocalAnalyticsData(viewModel);
            }
        }
        catch (Exception ex)
        {
            // Log error but return partial data if available
            _logger.LogError(ex, "Error retrieving analytics data");
        }

        return viewModel;
    }

    /// <summary>
    /// Retrieves analytics data from Azure Application Insights
    /// </summary>
    private async Task GetAzureAnalyticsDataAsync(AnalyticsViewModel viewModel)
    {
        // Calculate time range
        var timeRange = CalculateQueryTimeRange(viewModel.StartDate!.Value, viewModel.EndDate!.Value);

        // Get page views for the specified date range
        viewModel.PageViews = await GetPageViewsAsync(viewModel.StartDate.Value, viewModel.EndDate.Value, timeRange);
        viewModel.TotalPageViews = viewModel.PageViews.Sum(pv => pv.ViewCount);

        // Get PDF downloads
        viewModel.PdfDownloads = await GetPdfDownloadsAsync(viewModel.StartDate.Value, viewModel.EndDate.Value, timeRange);
        viewModel.TotalPdfDownloads = viewModel.PdfDownloads.Sum(d => d.DownloadCount);

        // Get search terms from resource guide
        viewModel.SearchTerms = await GetSearchTermsAsync(viewModel.StartDate.Value, viewModel.EndDate.Value, timeRange);
        viewModel.TotalSearches = viewModel.SearchTerms.Sum(st => st.SearchCount);

        // Get unique user count
        viewModel.TotalUniqueUsers = await GetUniqueUsersAsync(viewModel.StartDate.Value, viewModel.EndDate.Value, timeRange);
    }

    /// <summary>
    /// Note: This provides simulated/sample data for local development
    /// </summary>
    private void GetLocalAnalyticsData(AnalyticsViewModel viewModel)
    {
        _logger.LogDebug("Providing sample analytics data for local development");
        
        // Sample page views
        viewModel.PageViews = new List<PageViewData>
        {
            new PageViewData { PageUrl = "/Home/Index", ViewCount = 45 },
            new PageViewData { PageUrl = "/Resources/ResourceGuide", ViewCount = 32 },
            new PageViewData { PageUrl = "/Events/Index", ViewCount = 28 },
            new PageViewData { PageUrl = "/Home/About", ViewCount = 21 },
            new PageViewData { PageUrl = "/Resources/FocusNewsletters", ViewCount = 18 },
            new PageViewData { PageUrl = "/Admin/Analytics", ViewCount = 5 }
        };
        viewModel.TotalPageViews = viewModel.PageViews.Sum(pv => pv.ViewCount);

        // Sample PDF downloads
        viewModel.PdfDownloads = new List<DownloadData>
        {
            new DownloadData 
            { 
                FileName = "January-2025-Newsletter.pdf", 
                FileUrl = "https://example.blob.core.windows.net/files/January-2025-Newsletter.pdf",
                DownloadCount = 12 
            },
            new DownloadData 
            { 
                FileName = "December-2024-Newsletter.pdf", 
                FileUrl = "https://example.blob.core.windows.net/files/December-2024-Newsletter.pdf",
                DownloadCount = 8 
            },
            new DownloadData 
            { 
                FileName = "November-2024-Newsletter.pdf", 
                FileUrl = "https://example.blob.core.windows.net/files/November-2024-Newsletter.pdf",
                DownloadCount = 5 
            }
        };
        viewModel.TotalPdfDownloads = viewModel.PdfDownloads.Sum(d => d.DownloadCount);

        // Sample search terms
        viewModel.SearchTerms = new List<SearchTermData>
        {
            new SearchTermData { SearchType = "City", SearchTerm = "Tacoma", SearchCount = 15 },
            new SearchTermData { SearchType = "Service", SearchTerm = "Housing Support", SearchCount = 12 },
            new SearchTermData { SearchType = "Agency", SearchTerm = "Community Services", SearchCount = 9 },
            new SearchTermData { SearchType = "City", SearchTerm = "Puyallup", SearchCount = 7 },
            new SearchTermData { SearchType = "Service", SearchTerm = "Employment Services", SearchCount = 6 },
            new SearchTermData { SearchType = "CityAndCategory", SearchTerm = "Tacoma - Education", SearchCount = 4 }
        };
        viewModel.TotalSearches = viewModel.SearchTerms.Sum(st => st.SearchCount);

        // Sample unique users count
        viewModel.TotalUniqueUsers = 42;
    }

    /// <summary>
    /// Calculates the QueryTimeRange based on start and end dates
    /// </summary>
    private QueryTimeRange CalculateQueryTimeRange(DateTime startDate, DateTime endDate)
    {
        // Convert to UTC if not already
        var startUtc = startDate.Kind == DateTimeKind.Utc ? startDate : startDate.ToUniversalTime();
        var endUtc = endDate.Kind == DateTimeKind.Utc ? endDate : endDate.ToUniversalTime();

        return new QueryTimeRange(startUtc, endUtc);
    }

    /// <summary>
    /// Retrieves page view data for the specified date range from Azure
    /// </summary>
    private async Task<List<PageViewData>> GetPageViewsAsync(DateTime startDate, DateTime endDate, QueryTimeRange timeRange)
    {
        var pageViews = new List<PageViewData>();

        if (_logsQueryClient == null)
            return pageViews;

        try
        {
            // Use the same filtering as Azure Portal's default metrics
            // This matches the "Browser" view which filters out non-browser clients
            // Filter out old server side tracking by ensuring page view does not start with "/"
            // This filter can be safely removed in the future once the old server data falls off.
            var query = $@"
                    AppPageViews
                    | where TimeGenerated between (datetime({startDate}) .. datetime({endDate}))
                    | where isempty(SyntheticSource)
                    | where ClientType == 'Browser' or ClientType == 'PC'
                    | where isnotempty(UserId)  // Only count users with valid IDs
                    | summarize UniqueUsers = dcount(UserId)";

            Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                _workspaceId,
                query,
                timeRange);

            if (response.Value.Status == LogsQueryResultStatus.Success)
            {
                var table = response.Value.Table;
                foreach (var row in table.Rows)
                {
                    pageViews.Add(new PageViewData
                    {
                        PageUrl = row[0]?.ToString() ?? string.Empty,
                        ViewCount = int.TryParse(row[1]?.ToString(), out int count) ? count : 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving page views from Application Insights");
        }

        return pageViews;
    }

    /// <summary>
    /// Retrieves PDF download events from custom telemetry for the specified date range from Azure
    /// </summary>
    private async Task<List<DownloadData>> GetPdfDownloadsAsync(DateTime startDate, DateTime endDate, QueryTimeRange timeRange)
    {
        var downloads = new List<DownloadData>();

        if (_logsQueryClient == null)
            return downloads;

        try
        {
            var query = $@"
                    AppEvents
                    | where TimeGenerated between (datetime({startDate:yyyy-MM-ddTHH:mm:ssZ}) .. datetime({endDate:yyyy-MM-ddTHH:mm:ssZ}))
                    | where Name == 'FocusNewsletter'
                    | where isempty(SyntheticSource)
                    | where ClientType == 'Browser' or ClientType == 'PC'
                    | extend linkUrl = tostring(Properties.linkUrl)
                    | summarize DownloadCount = count() by linkUrl
                    | order by DownloadCount desc";

            Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                _workspaceId,
                query,
                timeRange);

            if (response.Value.Status == LogsQueryResultStatus.Success)
            {
                var table = response.Value.Table;
                foreach (var row in table.Rows)
                {
                    var url = row[0]?.ToString() ?? string.Empty;
                    var fileName = ExtractFileNameFromUrl(url);
                    
                    downloads.Add(new DownloadData
                    {
                        FileUrl = url,
                        FileName = fileName,
                        DownloadCount = int.TryParse(row[1]?.ToString(), out int count) ? count : 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF downloads from Application Insights");
        }

        return downloads;
    }

    /// <summary>
    /// Retrieves search term data from ResourceGuideSearch custom events for the specified date range from Azure
    /// </summary>
    private async Task<List<SearchTermData>> GetSearchTermsAsync(DateTime startDate, DateTime endDate, QueryTimeRange timeRange)
    {
        var searchTerms = new List<SearchTermData>();

        if (_logsQueryClient == null)
            return searchTerms;

        try
        {
            var query = $@"
                    AppEvents
                    | where TimeGenerated between (datetime({startDate:yyyy-MM-ddTHH:mm:ssZ}) .. datetime({endDate:yyyy-MM-ddTHH:mm:ssZ}))
                    | where Name == 'ResourceGuideSearch'
                    | where isempty(SyntheticSource)
                    | where ClientType == 'Browser' or ClientType == 'PC'
                    | extend SearchType = tostring(Properties.SearchType)
                    | extend SearchTerm = tostring(Properties.SearchTerm)
                    | summarize SearchCount = count() by SearchType, SearchTerm
                    | order by SearchCount desc
                    | limit 100";

            Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                _workspaceId,
                query,
                timeRange);

            if (response.Value.Status == LogsQueryResultStatus.Success)
            {
                var table = response.Value.Table;
                foreach (var row in table.Rows)
                {
                    searchTerms.Add(new SearchTermData
                    {
                        SearchType = row[0]?.ToString() ?? string.Empty,
                        SearchTerm = row[1]?.ToString() ?? string.Empty,
                        SearchCount = int.TryParse(row[2]?.ToString(), out int count) ? count : 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving search terms from Application Insights");
        }

        return searchTerms;
    }

    /// <summary>
    /// Retrieves the count of unique users for the specified date range from Azure
    /// </summary>
    private async Task<int> GetUniqueUsersAsync(DateTime startDate, DateTime endDate, QueryTimeRange timeRange)
    {
        if (_logsQueryClient == null)
            return 0;

        try
        {
            var query = $@"
                    AppPageViews
                    | where TimeGenerated between (datetime({startDate:yyyy-MM-ddTHH:mm:ssZ}) .. datetime({endDate:yyyy-MM-ddTHH:mm:ssZ}))
                    | where isempty(SyntheticSource)
                    | where ClientType == 'Browser' or ClientType == 'PC'
                    | where isnotempty(UserId)  // Only count users with valid IDs
                    | summarize UniqueUsers = dcount(UserId)";

            Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                _workspaceId,
                query,
                timeRange);

            if (response.Value.Status == LogsQueryResultStatus.Success)
            {
                var table = response.Value.Table;
                if (table.Rows.Count > 0)
                {
                    var row = table.Rows[0];
                    return int.TryParse(row[0]?.ToString(), out int count) ? count : 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unique users from Application Insights");
        }

        return 0;
    }

    /// <summary>
    /// Extracts the file name from a URL
    /// </summary>
    private string ExtractFileNameFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            return segments.Length > 0 ? Uri.UnescapeDataString(segments[^1]) : url;
        }
        catch
        {
            return url;
        }
    }
}
