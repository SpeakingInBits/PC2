using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure;
using PC2.Models;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace PC2.Services
{
    /// <summary>
    /// Service for querying Application Insights telemetry data
    /// </summary>
    public class AnalyticsService
    {
        private readonly LogsQueryClient? _logsQueryClient;
        private readonly string? _workspaceId;
        private readonly bool _isConfigured;

        public AnalyticsService(IConfiguration configuration)
        {
            _workspaceId = configuration["ApplicationInsights:WorkspaceId"];
            var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

            // Only initialize client if both workspace ID and connection string are configured
            if (!string.IsNullOrEmpty(_workspaceId) && !string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    // Create the logs query client using DefaultAzureCredential
                    // This will use Azure CLI credentials, managed identity, or environment variables
                    _logsQueryClient = new LogsQueryClient(new DefaultAzureCredential());
                    _isConfigured = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize Application Insights client: {ex.Message}");
                    _isConfigured = false;
                }
            }
            else
            {
                _isConfigured = false;
            }
        }

        /// <summary>
        /// Retrieves analytics data for the past 30 days including page views, downloads, and search terms
        /// </summary>
        /// <returns>AnalyticsViewModel populated with data from Application Insights</returns>
        public async Task<AnalyticsViewModel> GetAnalyticsDataAsync()
        {
            var viewModel = new AnalyticsViewModel();

            if (!_isConfigured || _logsQueryClient == null || string.IsNullOrEmpty(_workspaceId))
            {
                return viewModel;
            }

            try
            {
                // Get page views for the past 30 days
                viewModel.PageViews = await GetPageViewsAsync();
                viewModel.TotalPageViews = viewModel.PageViews.Sum(pv => pv.ViewCount);

                // Get PDF downloads
                viewModel.PdfDownloads = await GetPdfDownloadsAsync();
                viewModel.TotalPdfDownloads = viewModel.PdfDownloads.Sum(d => d.DownloadCount);

                // Get search terms from resource guide
                viewModel.SearchTerms = await GetSearchTermsAsync();
                viewModel.TotalSearches = viewModel.SearchTerms.Sum(st => st.SearchCount);
            }
            catch (Exception ex)
            {
                // Log error but return partial data if available
                Console.WriteLine($"Error retrieving analytics data: {ex.Message}");
            }

            return viewModel;
        }

        /// <summary>
        /// Retrieves page view data for the past 30 days
        /// </summary>
        private async Task<List<PageViewData>> GetPageViewsAsync()
        {
            var pageViews = new List<PageViewData>();

            if (_logsQueryClient == null || string.IsNullOrEmpty(_workspaceId))
                return pageViews;

            try
            {
                var query = @"
                    pageViews
                    | where timestamp > ago(30d)
                    | summarize ViewCount = count() by name
                    | order by ViewCount desc
                    | limit 50";

                Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                    _workspaceId,
                    query,
                    new QueryTimeRange(TimeSpan.FromDays(30)));

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
        /// Retrieves PDF download events from custom telemetry
        /// </summary>
        private async Task<List<DownloadData>> GetPdfDownloadsAsync()
        {
            var downloads = new List<DownloadData>();

            if (_logsQueryClient == null || string.IsNullOrEmpty(_workspaceId))
                return downloads;

            try
            {
                var query = @"
                    customEvents
                    | where timestamp > ago(30d)
                    | where name == 'FocusNewsletter'
                    | extend linkUrl = tostring(customDimensions.linkUrl)
                    | summarize DownloadCount = count() by linkUrl
                    | order by DownloadCount desc";

                Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                    _workspaceId,
                    query,
                    new QueryTimeRange(TimeSpan.FromDays(30)));

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
        /// Retrieves search term data from ResourceGuideSearch custom events
        /// </summary>
        private async Task<List<SearchTermData>> GetSearchTermsAsync()
        {
            var searchTerms = new List<SearchTermData>();

            if (_logsQueryClient == null || string.IsNullOrEmpty(_workspaceId))
                return searchTerms;

            try
            {
                var query = @"
                    customEvents
                    | where timestamp > ago(30d)
                    | where name == 'ResourceGuideSearch'
                    | extend SearchType = tostring(customDimensions.SearchType)
                    | extend SearchTerm = tostring(customDimensions.SearchTerm)
                    | summarize SearchCount = count() by SearchType, SearchTerm
                    | order by SearchCount desc
                    | limit 100";

                Response<LogsQueryResult> response = await _logsQueryClient.QueryWorkspaceAsync(
                    _workspaceId,
                    query,
                    new QueryTimeRange(TimeSpan.FromDays(30)));

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
