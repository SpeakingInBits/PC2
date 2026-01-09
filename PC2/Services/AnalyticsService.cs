using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure;
using PC2.Models;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace PC2.Services
{
    /// <summary>
    /// Service for querying Application Insights telemetry data from Azure or local development
    /// </summary>
    public class AnalyticsService
    {
        private readonly LogsQueryClient? _logsQueryClient;
        private readonly TelemetryClient? _telemetryClient;
        private readonly string? _workspaceId;
        private readonly bool _isAzureConfigured;
        private readonly IConfiguration _configuration;

        public AnalyticsService(IConfiguration configuration, TelemetryClient? telemetryClient = null)
        {
            _configuration = configuration;
            _telemetryClient = telemetryClient;
            _workspaceId = configuration["ApplicationInsights:WorkspaceId"];
            var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

            // Only initialize Azure client if both workspace ID and connection string are configured
            if (!string.IsNullOrEmpty(_workspaceId) && !string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    // Create the logs query client using DefaultAzureCredential
                    // This will use Azure CLI credentials, managed identity, or environment variables
                    _logsQueryClient = new LogsQueryClient(new DefaultAzureCredential());
                    _isAzureConfigured = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize Azure Application Insights client: {ex.Message}");
                    Console.WriteLine("Falling back to local Application Insights data");
                    _isAzureConfigured = false;
                }
            }
            else
            {
                // Use local Application Insights telemetry
                Console.WriteLine("Azure Application Insights not configured. Using local telemetry data.");
                _isAzureConfigured = false;
            }
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
                if (_isAzureConfigured && _logsQueryClient != null && !string.IsNullOrEmpty(_workspaceId))
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
                Console.WriteLine($"Error retrieving analytics data: {ex.Message}");
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
        }

        /// <summary>
        /// Retrieves analytics data from local Application Insights telemetry (development mode)
        /// Note: This provides simulated/sample data for local development
        /// </summary>
        private void GetLocalAnalyticsData(AnalyticsViewModel viewModel)
        {
            // For local development, we'll provide sample data
            // In a real implementation, you could query the local telemetry channel or in-memory storage
            
            Console.WriteLine("Providing sample analytics data for local development");
            
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

            if (_logsQueryClient == null || string.IsNullOrEmpty(_workspaceId))
                return pageViews;

            try
            {
                // Use the same filtering as Azure Portal's default metrics
                // This matches the "Browser" view which filters out non-browser clients
                var query = $@"
                    pageViews
                    | where timestamp >= datetime({startDate:yyyy-MM-ddTHH:mm:ssZ})
                    | where timestamp <= datetime({endDate:yyyy-MM-ddTHH:mm:ssZ})
                    | where isempty(operation_SyntheticSource)
                    | where client_Type == 'Browser' or client_Type == 'PC'
                    | summarize ViewCount = count() by name
                    | order by ViewCount desc
                    | limit 50";

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
                Console.WriteLine($"Error retrieving page views: {ex.Message}");
            }

            return pageViews;
        }

        /// <summary>
        /// Retrieves PDF download events from custom telemetry for the specified date range from Azure
        /// </summary>
        private async Task<List<DownloadData>> GetPdfDownloadsAsync(DateTime startDate, DateTime endDate, QueryTimeRange timeRange)
        {
            var downloads = new List<DownloadData>();

            if (_logsQueryClient == null || string.IsNullOrEmpty(_workspaceId))
                return downloads;

            try
            {
                var query = $@"
                    customEvents
                    | where timestamp >= datetime({startDate:yyyy-MM-ddTHH:mm:ssZ})
                    | where timestamp <= datetime({endDate:yyyy-MM-ddTHH:mm:ssZ})
                    | where name == 'FocusNewsletter'
                    | where isempty(operation_SyntheticSource)
                    | where client_Type == 'Browser' or client_Type == 'PC'
                    | extend linkUrl = tostring(customDimensions.linkUrl)
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
                Console.WriteLine($"Error retrieving PDF downloads: {ex.Message}");
            }

            return downloads;
        }

        /// <summary>
        /// Retrieves search term data from ResourceGuideSearch custom events for the specified date range from Azure
        /// </summary>
        private async Task<List<SearchTermData>> GetSearchTermsAsync(DateTime startDate, DateTime endDate, QueryTimeRange timeRange)
        {
            var searchTerms = new List<SearchTermData>();

            if (_logsQueryClient == null || string.IsNullOrEmpty(_workspaceId))
                return searchTerms;

            try
            {
                var query = $@"
                    customEvents
                    | where timestamp >= datetime({startDate:yyyy-MM-ddTHH:mm:ssZ})
                    | where timestamp <= datetime({endDate:yyyy-MM-ddTHH:mm:ssZ})
                    | where name == 'ResourceGuideSearch'
                    | where isempty(operation_SyntheticSource)
                    | where client_Type == 'Browser' or client_Type == 'PC'
                    | extend SearchType = tostring(customDimensions.SearchType)
                    | extend SearchTerm = tostring(customDimensions.SearchTerm)
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
                Console.WriteLine($"Error retrieving search terms: {ex.Message}");
            }

            return searchTerms;
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
}
