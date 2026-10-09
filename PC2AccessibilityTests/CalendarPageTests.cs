using Microsoft.Playwright;

namespace PC2AccessibilityTests;

/// <summary>
/// Scans the parts of the admin calendar pages that only appear after interacting with them,
/// since axe skips hidden content.
/// </summary>
[TestClass]
[TestCategory("Accessibility")]
public class CalendarPageTests
{
    /// <summary>
    /// The "Repeats" choice shows different fields and loads a preview of the dates
    /// </summary>
    [TestMethod]
    [DataRow("Weekly")]
    [DataRow("MonthlyByWeekday")]
    [DataRow("SpecificDates")]
    public async Task CreateEvent_RepeatOptions_MeetWcag(string repeat)
    {
        IPage page = await AccessibilityTestSite.NewAdminPageAsync();
        try
        {
            // The 1st of next month is never a fifth weekday, so every option is available
            DateTime date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
            await page.GotoAsync($"/Calendar/Create?date={date:yyyy-MM-dd}");
            await page.SelectOptionAsync("#Recurrence_Repeat", repeat);

            if (repeat == "SpecificDates")
            {
                await page.ClickAsync("[data-recurrence-add-date]");
                await page.FillAsync(".recurrence-specific-date", date.AddDays(7).ToString("yyyy-MM-dd"));
                await page.DispatchEventAsync(".recurrence-specific-date", "change");
            }

            await page.WaitForSelectorAsync("[data-recurrence-date]");

            await AccessibilityScanner.AssertNoViolationsAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [TestMethod]
    [DataRow(".fc-dayGridMonth-button")]
    [DataRow(".fc-listMonth-button")]
    public async Task Calendar_EventPanel_MeetsWcag(string viewButton)
    {
        IPage page = await AccessibilityTestSite.NewAdminPageAsync();
        try
        {
            await page.GotoAsync("/Calendar");
            await page.ClickAsync(viewButton);
            await page.WaitForSelectorAsync(viewButton + ".fc-button-active");

            ILocator firstEvent = page.Locator(".fc-event").First;
            if (await page.Locator(".fc-event").CountAsync() == 0)
            {
                Assert.Inconclusive("There are no events this month to open. Add an event and run the test again.");
            }

            await firstEvent.ClickAsync();
            // Wait for the panel's fade in to finish, so colours are checked at full strength
            await page.WaitForFunctionAsync(
                "() => getComputedStyle(document.getElementById('event-actions')).opacity === '1'");

            await AccessibilityScanner.AssertNoViolationsAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [TestMethod]
    public async Task EditSeries_WithScheduleShown_MeetsWcag()
    {
        IPage page = await AccessibilityTestSite.NewAdminPageAsync();
        try
        {
            await page.GotoAsync("/Calendar");

            // The events on the calendar page include the id of each event's series
            int? seriesId = await page.EvaluateAsync<int?>(
                "JSON.parse(document.getElementById('calendar-events').textContent)" +
                ".map(e => e.extendedProps.seriesId).find(id => id != null) ?? null");
            if (seriesId == null)
            {
                Assert.Inconclusive("There are no repeating events. Add one and run the test again.");
            }

            await page.GotoAsync($"/Calendar/EditSeries/{seriesId}");
            await page.CheckAsync("#ChangeSchedule");
            await page.WaitForSelectorAsync("[data-recurrence-date]");

            await AccessibilityScanner.AssertNoViolationsAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
