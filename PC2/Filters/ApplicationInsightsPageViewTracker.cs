using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PC2.Filters
{
    public class ApplicationInsightsPageViewTracker : IAsyncResultFilter
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsPageViewTracker(TelemetryClient telemetryClient)
        {
            this._telemetryClient = telemetryClient;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Get current URL
            string url = context.HttpContext.Request.Path;
            _telemetryClient.TrackPageView(url);
            await next();
        }
    }
}
