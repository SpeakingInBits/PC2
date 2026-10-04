using Microsoft.Playwright;

namespace PC2AccessibilityTests;

/// <summary>
/// Scans the pages anyone can visit without logging in.
/// </summary>
[TestClass]
[TestCategory("Accessibility")]
public class PublicPageTests
{
    [TestMethod]
    [DataRow("/")]
    [DataRow("/Home/About")]
    [DataRow("/Home/HousingProgram")]
    [DataRow("/Home/PersonCenteredPlanning")]
    [DataRow("/Home/Privacy")]
    [DataRow("/Home/ContactPage")]
    [DataRow("/GetHelp")]
    [DataRow("/GetHelp/Self")]
    [DataRow("/GetHelp/Professional")]
    [DataRow("/GetHelp/Thanks")]
    [DataRow("/Resources")]
    [DataRow("/Resources/ResourceGuide")]
    [DataRow("/Resources/DisabilityAwareness")]
    [DataRow("/Resources/ResourceLinks")]
    [DataRow("/Resources/AgeSpecificIssues")]
    [DataRow("/Resources/LegislativeLinksAndEvents")]
    [DataRow("/Resources/EmergencyPreparedness")]
    [DataRow("/Resources/VirtualCloset")]
    [DataRow("/Events")]
    [DataRow("/NewsToKnow")]
    [DataRow("/ProgramVideos")]
    [DataRow("/Jobs")]
    [DataRow("/Identity/Account/Login")]
    public async Task Page_MeetsWcag(string path)
    {
        IPage page = await AccessibilityTestSite.NewPageAsync();
        await AccessibilityScanner.AssertPageIsAccessibleAsync(page, path);
    }

    /// <summary>
    /// The referral forms hide sections until an answer shows them, and axe skips hidden content
    /// </summary>
    [TestMethod]
    [DataRow("/GetHelp/Self", "#ReferringFor-0")]
    [DataRow("/GetHelp/Self", "#ReferringFor-1")]
    [DataRow("/GetHelp/Self", "#ReferringFor-2")]
    [DataRow("/GetHelp/Professional", "#HasConsent-0")]
    public async Task GetHelp_SectionsShownByAnswers_MeetWcag(string path, string answerSelector)
    {
        IPage page = await AccessibilityTestSite.NewPageAsync();
        try
        {
            await page.GotoAsync(path);
            await page.CheckAsync(answerSelector);

            await AccessibilityScanner.AssertNoViolationsAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [TestMethod]
    public async Task ResourceGuide_SearchResults_MeetWcag()
    {
        IPage page = await AccessibilityTestSite.NewPageAsync();
        try
        {
            await page.GotoAsync("/Resources/ResourceGuide");

            // An empty city/service search lists every agency, which also shows the feedback form
            await page.ClickAsync("input[type=submit][value='Search by city and/or service']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await AccessibilityScanner.AssertNoViolationsAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
