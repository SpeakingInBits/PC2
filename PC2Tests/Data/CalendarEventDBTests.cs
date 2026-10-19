using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class CalendarEventDBTests
{
    /// <summary>
    /// A clock stopped at a set UTC time
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [TestMethod]
    [DataRow("2026-10-23T03:00:00Z", "2026-10-22", DisplayName = "8 PM PDT is still the same day, though UTC is the next day")]
    [DataRow("2026-10-22T15:00:00Z", "2026-10-22", DisplayName = "8 AM PDT")]
    [DataRow("2026-10-23T07:30:00Z", "2026-10-23", DisplayName = "12:30 AM PDT is the next day")]
    [DataRow("2026-12-02T07:30:00Z", "2026-12-01", DisplayName = "11:30 PM PST in winter (UTC-8)")]
    public void Today_IsTheDateInPacificTime(string utcNow, string expectedDate)
    {
        var clock = new FixedTimeProvider(DateTimeOffset.Parse(utcNow));

        Assert.AreEqual(DateOnly.Parse(expectedDate), CalendarEventDB.Today(clock));
    }

    [TestMethod]
    public async Task DeletePastEvents_RemovesOnlyPastEvents()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        DateOnly today = CalendarEventDB.Today();
        foreach (int days in new[] { -10, -1, 0, 1 })
        {
            context.CalendarEvents.Add(new CalendarEvent
            {
                DateOfEvent = today.AddDays(days),
                StartingTime = new TimeOnly(10, 0),
                EndingTime = new TimeOnly(11, 0),
                EventDescription = $"Event {days}",
                PC2Event = true
            });
        }
        await context.SaveChangesAsync();

        await CalendarEventDB.DeletePastEvents(context);

        CollectionAssert.AreEquivalent(new[] { today, today.AddDays(1) },
            await context.CalendarEvents.Select(e => e.DateOfEvent).ToListAsync());
    }
}
