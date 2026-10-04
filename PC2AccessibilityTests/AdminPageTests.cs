using Microsoft.Playwright;

namespace PC2AccessibilityTests;

/// <summary>
/// Scans the Admin and Staff pages, logged in as the default admin.
/// </summary>
[TestClass]
[TestCategory("Accessibility")]
public class AdminPageTests
{
    [TestMethod]
    [DataRow("/Home/Dashboard")]
    [DataRow("/Admin/Analytics")]
    [DataRow("/About/HousingProgramData")]
    [DataRow("/About/UploadNewsletter")]
    [DataRow("/Agency")]
    [DataRow("/Agency/Create")]
    [DataRow("/AgencyCategory/Manage")]
    [DataRow("/AgencyCategory/Create")]
    [DataRow("/Calendar")]
    [DataRow("/Calendar/Create")]
    [DataRow("/Feedback")]
    [DataRow("/Jobs/Manage")]
    [DataRow("/Jobs/Create")]
    [DataRow("/People")]
    [DataRow("/People/Create")]
    [DataRow("/ProgramVideos/ManageVideos")]
    [DataRow("/ProgramVideos/CreateVideo")]
    [DataRow("/ResourceLinks/Manage")]
    [DataRow("/ResourceLinks/Create")]
    [DataRow("/UserManagement")]
    [DataRow("/UserManagement/Create")]
    [DataRow("/Identity/Account/Manage")]
    public async Task Page_MeetsWcag(string path)
    {
        IPage page = await AccessibilityTestSite.NewAdminPageAsync();
        await AccessibilityScanner.AssertPageIsAccessibleAsync(page, path);
    }
}
