using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights.DataContracts;
using System.Threading.Tasks;
using PC2.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class AnalyticsController : Controller
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IConfiguration _configuration;

        public AnalyticsController(TelemetryClient telemetryClient, IConfiguration configuration)
        {
            _telemetryClient = telemetryClient;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var pageViews = await GetHistoricalData("PageViews");
            var uniqueVisitors = await GetHistoricalData("UniqueVisitors");
            var sessionDuration = await GetHistoricalData("SessionDuration");
            var bounceRate = await GetHistoricalData("BounceRate");

            ViewData["PageViews"] = pageViews;
            ViewData["UniqueVisitors"] = uniqueVisitors;
            ViewData["SessionDuration"] = sessionDuration;
            ViewData["BounceRate"] = bounceRate;

            return View();
        }

        private async Task<double> GetMetricData(string metricName)
        {
            var request = new MetricTelemetry(metricName, 1);
            _telemetryClient.TrackMetric(request);
            return await Task.FromResult(request.Sum);
        }

        private async Task<JObject> GetHistoricalData(string metricName)
        {
            string connectionString = _configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            string requestUri = $"https://api.applicationinsights.io/v1/apps/{_configuration["ApplicationInsightsAppId"]}/metrics/{metricName}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-api-key", connectionString);

                HttpResponseMessage response = await client.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(data);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
