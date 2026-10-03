using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace PC2AccessibilityTests;

/// <summary>
/// Runs axe-core (https://github.com/dequelabs/axe-core) against a page and fails the test
/// with a readable list of problems when the page doesn't meet WCAG 2.2 AA.
/// </summary>
public static class AccessibilityScanner
{
    /// <summary>
    /// The WCAG levels the website is expected to meet.
    /// </summary>
    private static readonly List<string> WcagTags =
        ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"];

    /// <summary>
    /// Opens <paramref name="path"/> in a page from <see cref="AccessibilityTestSite"/>, scans it,
    /// and closes the page's browser context.
    /// </summary>
    public static async Task AssertPageIsAccessibleAsync(IPage page, string path)
    {
        try
        {
            IResponse? response = await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            Assert.IsNotNull(response, $"No response from {path}");
            Assert.IsTrue(response.Ok, $"{path} returned HTTP {response.Status}");

            await AssertNoViolationsAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    /// <summary>
    /// Scans the page in its current state. Use this after interacting with the page,
    /// e.g. submitting a search or opening a dialog.
    /// </summary>
    public static async Task AssertNoViolationsAsync(IPage page)
    {
        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = WcagTags }
        };
        AxeResult result = await page.RunAxe(options);

        if (result.Violations.Length > 0)
        {
            Assert.Fail(FormatViolations(page.Url, result.Violations));
        }
    }

    private static string FormatViolations(string url, AxeResultItem[] violations)
    {
        var message = new StringBuilder();
        int issueCount = violations.Sum(violation => violation.Nodes.Length);
        message.AppendLine($"{issueCount} accessibility issue(s) on {url}");

        foreach (AxeResultItem violation in violations)
        {
            message.AppendLine();
            message.AppendLine($"[{violation.Impact}] {violation.Id}: {violation.Help}");
            message.AppendLine($"  WCAG: {string.Join(", ", violation.Tags.Where(tag => tag.StartsWith("wcag")))}");
            message.AppendLine($"  How to fix: {violation.HelpUrl}");

            foreach (AxeResultNode node in violation.Nodes)
            {
                message.AppendLine($"  - Element: {node.Target}");
                message.AppendLine($"    HTML: {Truncate(node.Html, 200)}");
                AppendChecks(message, "Fix all of the following:", [.. node.All, .. node.None]);
                AppendChecks(message, "Fix any of the following:", node.Any);
            }
        }
        return message.ToString();
    }

    private static void AppendChecks(StringBuilder message, string heading, AxeResultCheck[] checks)
    {
        if (checks.Length == 0)
        {
            return;
        }

        message.AppendLine($"    {heading}");
        foreach (AxeResultCheck check in checks)
        {
            message.AppendLine($"      * {check.Message}");
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
