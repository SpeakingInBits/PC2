using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;
using PC2.Services;

namespace PC2.Controllers;

/// <summary>
/// Controller for admin-only features including analytics dashboard
/// </summary>
[Authorize(Roles = IdentityHelper.Admin)]
public class AdminController : Controller
{
    private readonly AnalyticsService _analyticsService;

    public AdminController(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Displays analytics dashboard showing page views, PDF downloads, and search terms for the specified date range
    /// </summary>
    /// <param name="startDate">Start date for the analytics filter</param>
    /// <param name="endDate">End date for the analytics filter</param>
    /// <param name="month">Selected month (1-12) for month/year filter</param>
    /// <param name="year">Selected year for month/year filter</param>
    /// <returns>View with analytics data</returns>
    public async Task<IActionResult> Analytics(DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
    {
        try
        {
            // If month and year are provided, calculate the date range for that month
            if (month.HasValue && year.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                startDate = new DateTime(year.Value, month.Value, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            // If no dates provided, default to last 30 days
            else if (!startDate.HasValue || !endDate.HasValue)
            {
                endDate = DateTime.Now;
                startDate = endDate.Value.AddDays(-30);
            }
            // Ensure end date includes the full day
            else if (endDate.HasValue)
            {
                endDate = endDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            var analyticsData = await _analyticsService.GetAnalyticsDataAsync(startDate, endDate);
            
            // Set the filter values for the view to display
            analyticsData.StartDate = startDate;
            analyticsData.EndDate = endDate;
            analyticsData.SelectedMonth = month;
            analyticsData.SelectedYear = year;
            
            // If no data was returned, show configuration message
            if (analyticsData.TotalPageViews == 0 && 
                analyticsData.TotalPdfDownloads == 0 && 
                analyticsData.TotalSearches == 0 &&
                analyticsData.TotalUniqueUsers == 0)
            {
                ViewBag.InfoMessage = "No data found for the selected date range. " +
                    "Application Insights may not be configured, or there is no data for this period. " +
                    "Please ensure the WorkspaceId is set in appsettings.json and you have proper Azure credentials configured.";
            }
            
            return View(analyticsData);
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error retrieving analytics data: {ex.Message}";
            return View(new AnalyticsViewModel 
            { 
                StartDate = startDate ?? DateTime.Now.AddDays(-30),
                EndDate = endDate ?? DateTime.Now,
                SelectedMonth = month,
                SelectedYear = year
            });
        }
    }
}
