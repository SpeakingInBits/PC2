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
    /// Displays analytics dashboard showing page views, PDF downloads, and search terms from the past 30 days
    /// </summary>
    public async Task<IActionResult> Analytics()
    {
        try
        {
            var analyticsData = await _analyticsService.GetAnalyticsDataAsync();
            
            // If no data was returned, show configuration message
            if (analyticsData.TotalPageViews == 0 && 
                analyticsData.TotalPdfDownloads == 0 && 
                analyticsData.TotalSearches == 0)
            {
                ViewBag.InfoMessage = "Application Insights may not be configured or there is no data for the past 30 days. " +
                    "Please ensure the WorkspaceId is set in appsettings.json and you have proper Azure credentials configured.";
            }
            
            return View(analyticsData);
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Error retrieving analytics data: {ex.Message}";
            return View(new AnalyticsViewModel());
        }
    }
}
