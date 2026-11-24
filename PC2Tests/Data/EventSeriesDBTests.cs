using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class EventSeriesDBTests
{
    private ApplicationDbContext _context = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;

        _context = new ApplicationDbContext(_options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static EventSeries CreateWeeklySeries(DateOnly startDate)
    {
        return new EventSeries
        {
            Frequency = RecurrenceFrequency.Weekly,
            WeeklyDays = RecurrenceDays.Tuesday,
            StartDate = startDate,
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Support group",
            CountyEvent = true
        };
    }

    private static CalendarEvent CreateOneTimeEvent(DateOnly date)
    {
        return new CalendarEvent
        {
            DateOfEvent = date,
            StartingTime = new TimeOnly(9, 0),
            EndingTime = new TimeOnly(10, 0),
            EventDescription = "One time event",
            PC2Event = true
        };
    }

    #region AddSeries

    [TestMethod]
    public async Task AddSeries_CreatesAnEventForEachDate()
    {
        // Arrange
        EventSeries series = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        DateOnly[] dates = { new(2026, 10, 13), new(2026, 10, 20), new(2026, 10, 27) };

        // Act
        await EventSeriesDB.AddSeries(_context, series, dates);

        // Assert
        List<CalendarEvent> events = await _context.CalendarEvents.OrderBy(e => e.DateOfEvent).ToListAsync();
        Assert.HasCount(3, events);
        CollectionAssert.AreEqual(dates, events.Select(e => e.DateOfEvent).ToArray());
        Assert.IsTrue(events.All(e => e.EventSeriesID == series.EventSeriesID));
        Assert.IsTrue(events.All(e => e.EventDescription == "Support group" && e.CountyEvent && !e.PC2Event));
    }

    [TestMethod]
    public async Task AddSeries_WithDuplicateDates_CreatesOneEventPerDate()
    {
        // Arrange
        EventSeries series = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        DateOnly[] dates = { new(2026, 10, 20), new(2026, 10, 13), new(2026, 10, 20) };

        // Act
        await EventSeriesDB.AddSeries(_context, series, dates);

        // Assert
        Assert.AreEqual(2, await _context.CalendarEvents.CountAsync());
    }

    #endregion

    #region GetSeries

    [TestMethod]
    public async Task GetSeries_ReturnsSeriesWithEventsInDateOrder()
    {
        // Arrange
        EventSeries series = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        await EventSeriesDB.AddSeries(_context, series, new DateOnly[] { new(2026, 10, 27), new(2026, 10, 13) });
        _context.ChangeTracker.Clear();

        // Act
        EventSeries? result = await EventSeriesDB.GetSeries(_context, series.EventSeriesID);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result.Events);
        Assert.AreEqual(new DateOnly(2026, 10, 13), result.Events[0].DateOfEvent);
    }

    [TestMethod]
    public async Task GetSeries_WithMissingId_ReturnsNull()
    {
        Assert.IsNull(await EventSeriesDB.GetSeries(_context, 999));
    }

    #endregion

    #region DeleteSeries

    [TestMethod]
    public async Task DeleteSeries_RemovesSeriesAndItsEventsOnly()
    {
        // Arrange
        EventSeries series = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        await EventSeriesDB.AddSeries(_context, series, new DateOnly[] { new(2026, 10, 13), new(2026, 10, 20) });
        _context.CalendarEvents.Add(CreateOneTimeEvent(new DateOnly(2026, 10, 14)));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        await EventSeriesDB.DeleteSeries(_context, series.EventSeriesID);

        // Assert
        Assert.AreEqual(0, await _context.EventSeries.CountAsync());
        List<CalendarEvent> remaining = await _context.CalendarEvents.ToListAsync();
        Assert.HasCount(1, remaining);
        Assert.AreEqual("One time event", remaining[0].EventDescription);
    }

    #endregion

    #region ExtendOpenEndedSeries

    [TestMethod]
    public async Task ExtendOpenEndedSeries_AddsDatesAfterGeneratedThrough()
    {
        // Arrange - series was created through the end of October
        EventSeries series = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        series.GeneratedThrough = new DateOnly(2026, 10, 31);
        await EventSeriesDB.AddSeries(_context, series, new DateOnly[] { new(2026, 10, 13), new(2026, 10, 20), new(2026, 10, 27) });

        // Act - a month later
        await EventSeriesDB.ExtendOpenEndedSeries(_context, new DateOnly(2026, 11, 13));

        // Assert - every Tuesday through November 13, 2027, with no duplicates
        List<DateOnly> dates = await _context.CalendarEvents.Select(e => e.DateOfEvent).OrderBy(d => d).ToListAsync();
        Assert.AreEqual(new DateOnly(2026, 11, 3), dates[3]);
        Assert.AreEqual(new DateOnly(2027, 11, 9), dates[^1]);
        Assert.AreEqual(dates.Count, dates.Distinct().Count());
        Assert.AreEqual(new DateOnly(2027, 11, 13), series.GeneratedThrough);
    }

    [TestMethod]
    public async Task ExtendOpenEndedSeries_DoesNotRecreateDeletedDates()
    {
        // Arrange - October 20 was deleted after the series was created
        EventSeries series = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        series.GeneratedThrough = new DateOnly(2026, 10, 31);
        await EventSeriesDB.AddSeries(_context, series, new DateOnly[] { new(2026, 10, 13), new(2026, 10, 27) });

        // Act
        await EventSeriesDB.ExtendOpenEndedSeries(_context, new DateOnly(2026, 11, 13));

        // Assert
        Assert.IsFalse(await _context.CalendarEvents.AnyAsync(e => e.DateOfEvent == new DateOnly(2026, 10, 20)));
    }

    [TestMethod]
    public async Task ExtendOpenEndedSeries_WithNoGeneratedThrough_StartsFromStartDate()
    {
        // Arrange
        EventSeries series = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        await EventSeriesDB.AddSeries(_context, series, Array.Empty<DateOnly>());

        // Act
        await EventSeriesDB.ExtendOpenEndedSeries(_context, new DateOnly(2026, 10, 13));

        // Assert
        DateOnly first = await _context.CalendarEvents.MinAsync(e => e.DateOfEvent);
        Assert.AreEqual(new DateOnly(2026, 10, 13), first);
    }

    [TestMethod]
    public async Task ExtendOpenEndedSeries_IgnoresSeriesThatEnd()
    {
        // Arrange
        EventSeries withEndDate = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        withEndDate.EndDate = new DateOnly(2027, 6, 1);

        EventSeries withCount = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        withCount.OccurrenceCount = 10;

        EventSeries specificDates = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        specificDates.Frequency = RecurrenceFrequency.SpecificDates;

        await EventSeriesDB.AddSeries(_context, withEndDate, new DateOnly[] { new(2026, 10, 13) });
        await EventSeriesDB.AddSeries(_context, withCount, new DateOnly[] { new(2026, 10, 13) });
        await EventSeriesDB.AddSeries(_context, specificDates, new DateOnly[] { new(2026, 10, 13) });

        // Act
        await EventSeriesDB.ExtendOpenEndedSeries(_context, new DateOnly(2026, 10, 13));

        // Assert
        Assert.AreEqual(3, await _context.CalendarEvents.CountAsync());
    }

    #endregion

    #region DeleteFinishedSeries

    [TestMethod]
    public async Task DeleteFinishedSeries_RemovesOnlyEmptySeriesThatEnd()
    {
        // Arrange
        EventSeries endedAndEmpty = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        endedAndEmpty.OccurrenceCount = 2;

        EventSeries endedWithEvents = CreateWeeklySeries(new DateOnly(2026, 10, 13));
        endedWithEvents.OccurrenceCount = 2;

        EventSeries openEndedAndEmpty = CreateWeeklySeries(new DateOnly(2026, 10, 13));

        await EventSeriesDB.AddSeries(_context, endedAndEmpty, Array.Empty<DateOnly>());
        await EventSeriesDB.AddSeries(_context, endedWithEvents, new DateOnly[] { new(2026, 10, 13) });
        await EventSeriesDB.AddSeries(_context, openEndedAndEmpty, Array.Empty<DateOnly>());

        // Act
        await EventSeriesDB.DeleteFinishedSeries(_context);

        // Assert
        List<int> remaining = await _context.EventSeries.Select(s => s.EventSeriesID).ToListAsync();
        CollectionAssert.AreEquivalent(new[] { endedWithEvents.EventSeriesID, openEndedAndEmpty.EventSeriesID }, remaining);
    }

    [TestMethod]
    public async Task DeletePastEvents_RemovesSeriesWhoseDatesHavePassed()
    {
        // Arrange
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        EventSeries series = CreateWeeklySeries(today.AddDays(-14));
        series.EndDate = today.AddDays(-7);
        await EventSeriesDB.AddSeries(_context, series, new[] { today.AddDays(-14), today.AddDays(-7) });

        // Act
        await CalendarEventDB.DeletePastEvents(_context);

        // Assert
        Assert.AreEqual(0, await _context.CalendarEvents.CountAsync());
        Assert.AreEqual(0, await _context.EventSeries.CountAsync());
    }

    #endregion
}
