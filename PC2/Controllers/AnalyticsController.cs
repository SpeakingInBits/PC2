using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights.DataContracts;
using System.Threading.Tasks;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)]
    public class AnalyticsController : Controller
    {
        private readonly TelemetryClient _telemetryClient;

        public AnalyticsController(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public async Task<IActionResult> Index()
        {
            var pageViews = await GetMetricData("Page views");
            var uniqueVisitors = await GetMetricData("Unique visitors");
            var sessionDuration = await GetMetricData("Session duration");
            var bounceRate = await GetMetricData("Bounce rate");

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
    }
}
