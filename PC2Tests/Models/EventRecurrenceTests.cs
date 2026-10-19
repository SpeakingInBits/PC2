using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class EventRecurrenceTests
{
    private static readonly DateOnly FarFuture = new(2030, 1, 1);

    private static EventSeries CreateSeries(RecurrenceFrequency frequency, DateOnly startDate)
    {
        return new EventSeries
        {
            Frequency = frequency,
            StartDate = startDate,
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Support group",
            PC2Event = true
        };
    }

    private static void AssertDates(EventSeries series, params string[] expected)
    {
        List<DateOnly> actual = EventRecurrence.GetDates(series, series.StartDate, FarFuture).ToList();
        CollectionAssert.AreEqual(expected.Select(DateOnly.Parse).ToList(), actual);
    }

    #region Weekly

    [TestMethod]
    public void GetDates_WeeklySingleDay_RepeatsEachWeek()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));
        series.WeeklyDays = RecurrenceDays.Tuesday;
        series.OccurrenceCount = 4;

        AssertDates(series, "2026-10-13", "2026-10-20", "2026-10-27", "2026-11-03");
    }

    [TestMethod]
    public void GetDates_WeeklyMultipleDays_StartsOnFirstMatchingDayAfterStart()
    {
        // Starts on a Wednesday, so that week's Tuesday is skipped
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 14));
        series.WeeklyDays = RecurrenceDays.Tuesday | RecurrenceDays.Thursday;
        series.EndDate = new DateOnly(2026, 10, 31);

        AssertDates(series, "2026-10-15", "2026-10-20", "2026-10-22", "2026-10-27", "2026-10-29");
    }

    [TestMethod]
    public void GetDates_EveryOtherWeek_SkipsAlternateWeeks()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));
        series.WeeklyDays = RecurrenceDays.Tuesday | RecurrenceDays.Thursday;
        series.Interval = 2;
        series.OccurrenceCount = 6;

        AssertDates(series, "2026-10-13", "2026-10-15", "2026-10-27", "2026-10-29", "2026-11-10", "2026-11-12");
    }

    [TestMethod]
    public void GetDates_WeeklyWithNoDaysSelected_UsesStartDateWeekday()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));
        series.OccurrenceCount = 2;

        AssertDates(series, "2026-10-13", "2026-10-20");
    }

    #endregion

    #region Monthly

    [TestMethod]
    public void GetDates_MonthlySecondTuesday_RepeatsOnSecondTuesday()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByWeekday, new DateOnly(2026, 10, 13));
        series.WeekOfMonth = 2;
        series.OccurrenceCount = 4;

        AssertDates(series, "2026-10-13", "2026-11-10", "2026-12-08", "2027-01-12");
    }

    [TestMethod]
    public void GetDates_MonthlyLastTuesday_HandlesMonthsWithFourAndFiveTuesdays()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByWeekday, new DateOnly(2026, 10, 27));
        series.WeekOfMonth = EventSeries.LastWeekOfMonth;
        series.OccurrenceCount = 4;

        // December 2026 has five Tuesdays
        AssertDates(series, "2026-10-27", "2026-11-24", "2026-12-29", "2027-01-26");
    }

    [TestMethod]
    public void GetDates_MonthlyByWeekdayStartingOnFifthWeekday_DefaultsToLast()
    {
        // October 29, 2026 is the fifth Thursday of the month
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByWeekday, new DateOnly(2026, 10, 29));
        series.OccurrenceCount = 4;

        AssertDates(series, "2026-10-29", "2026-11-26", "2026-12-31", "2027-01-28");
    }

    [TestMethod]
    public void GetDates_MonthlyByDate31st_UsesLastDayOfShorterMonths()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByDate, new DateOnly(2026, 10, 31));
        series.OccurrenceCount = 5;

        AssertDates(series, "2026-10-31", "2026-11-30", "2026-12-31", "2027-01-31", "2027-02-28");
    }

    [TestMethod]
    public void GetDates_EveryOtherMonthByDate_SkipsAlternateMonths()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByDate, new DateOnly(2026, 10, 15));
        series.Interval = 2;
        series.OccurrenceCount = 3;

        AssertDates(series, "2026-10-15", "2026-12-15", "2027-02-15");
    }

    [TestMethod]
    public void GetDates_PatternThatRarelyMatches_StopsAtLimit()
    {
        // Fifth Thursday only in October, so the yearly repeat almost never matches
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByWeekday, new DateOnly(2026, 10, 29));
        series.WeekOfMonth = 5;
        series.Interval = 12;
        series.OccurrenceCount = 100;

        List<DateOnly> dates = EventRecurrence.GetDates(series, series.StartDate, new DateOnly(2028, 12, 31)).ToList();

        CollectionAssert.AreEqual(new List<DateOnly> { new(2026, 10, 29) }, dates);
    }

    #endregion

    #region End conditions

    [TestMethod]
    public void GetDates_EndDate_IsInclusive()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));
        series.WeeklyDays = RecurrenceDays.Tuesday;
        series.EndDate = new DateOnly(2026, 10, 27);

        AssertDates(series, "2026-10-13", "2026-10-20", "2026-10-27");
    }

    [TestMethod]
    public void GetDates_CountWithLaterFromDate_CountsFromStartDate()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));
        series.WeeklyDays = RecurrenceDays.Tuesday;
        series.OccurrenceCount = 3;

        List<DateOnly> dates = EventRecurrence.GetDates(series, new DateOnly(2026, 10, 21), FarFuture).ToList();

        CollectionAssert.AreEqual(new List<DateOnly> { new(2026, 10, 27) }, dates);
    }

    [TestMethod]
    public void GetDates_SpecificDates_ReturnsNoDates()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.SpecificDates, new DateOnly(2026, 10, 13));

        Assert.IsEmpty(EventRecurrence.GetDates(series, series.StartDate, FarFuture));
    }

    [TestMethod]
    public void GetGenerationLimit_OpenEnded_IsTwelveMonthsAhead()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));

        Assert.AreEqual(new DateOnly(2027, 10, 9), EventRecurrence.GetGenerationLimit(series, new DateOnly(2026, 10, 9)));
        Assert.IsTrue(series.IsOpenEnded);
    }

    [TestMethod]
    public void GetGenerationLimit_WithEndDate_IsEndDate()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));
        series.EndDate = new DateOnly(2028, 1, 1);

        Assert.AreEqual(new DateOnly(2028, 1, 1), EventRecurrence.GetGenerationLimit(series, new DateOnly(2026, 10, 9)));
        Assert.IsFalse(series.IsOpenEnded);
    }

    [TestMethod]
    public void GetDates_WithCountAndGenerationLimit_StopsAtCount()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByDate, new DateOnly(2026, 10, 15));
        series.OccurrenceCount = 24;

        DateOnly limit = EventRecurrence.GetGenerationLimit(series, new DateOnly(2026, 10, 9));

        Assert.HasCount(24, EventRecurrence.GetDates(series, series.StartDate, limit).ToList());
    }

    #endregion

    #region CreateOccurrence

    [TestMethod]
    public void CreateOccurrence_CopiesSeriesDetails()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));

        CalendarEvent occurrence = EventRecurrence.CreateOccurrence(series, new DateOnly(2026, 10, 20));

        Assert.AreEqual(new DateOnly(2026, 10, 20), occurrence.DateOfEvent);
        Assert.AreEqual(series.StartingTime, occurrence.StartingTime);
        Assert.AreEqual(series.EndingTime, occurrence.EndingTime);
        Assert.AreEqual(series.EventDescription, occurrence.EventDescription);
        Assert.IsTrue(occurrence.PC2Event);
        Assert.IsFalse(occurrence.CountyEvent);
        Assert.AreSame(series, occurrence.EventSeries);
    }

    #endregion

    #region Describe

    [TestMethod]
    public void Describe_EveryOtherWeekOnTwoDays()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 13));
        series.WeeklyDays = RecurrenceDays.Tuesday | RecurrenceDays.Thursday;
        series.Interval = 2;

        Assert.AreEqual("Every 2 weeks on Tuesday and Thursday", EventRecurrence.Describe(series));
    }

    [TestMethod]
    public void Describe_WeeklyOnThreeDays()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.Weekly, new DateOnly(2026, 10, 12));
        series.WeeklyDays = RecurrenceDays.Monday | RecurrenceDays.Wednesday | RecurrenceDays.Friday;

        Assert.AreEqual("Weekly on Monday, Wednesday and Friday", EventRecurrence.Describe(series));
    }

    [TestMethod]
    public void Describe_MonthlyByWeekdayWithCount()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByWeekday, new DateOnly(2026, 10, 13));
        series.WeekOfMonth = 2;
        series.OccurrenceCount = 6;

        Assert.AreEqual("Monthly on the second Tuesday, 6 times", EventRecurrence.Describe(series));
    }

    [TestMethod]
    public void Describe_MonthlyLastWeekday()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByWeekday, new DateOnly(2026, 10, 27));
        series.WeekOfMonth = EventSeries.LastWeekOfMonth;

        Assert.AreEqual("Monthly on the last Tuesday", EventRecurrence.Describe(series));
    }

    [TestMethod]
    public void Describe_MonthlyByDateWithEndDate()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.MonthlyByDate, new DateOnly(2026, 10, 15));
        series.EndDate = new DateOnly(2026, 12, 15);

        Assert.AreEqual("Monthly on day 15, until December 15, 2026", EventRecurrence.Describe(series));
    }

    [TestMethod]
    public void Describe_SpecificDates()
    {
        EventSeries series = CreateSeries(RecurrenceFrequency.SpecificDates, new DateOnly(2026, 10, 13));

        Assert.AreEqual("On specific dates", EventRecurrence.Describe(series));
    }

    #endregion

    #region Week of month helpers

    [TestMethod]
    public void DefaultWeekOfMonth_ReturnsWeekNumberOrLast()
    {
        Assert.AreEqual(2, EventRecurrence.DefaultWeekOfMonth(new DateOnly(2026, 10, 13)));
        Assert.AreEqual(4, EventRecurrence.DefaultWeekOfMonth(new DateOnly(2026, 10, 27)));
        Assert.AreEqual(EventSeries.LastWeekOfMonth, EventRecurrence.DefaultWeekOfMonth(new DateOnly(2026, 10, 29)));
    }

    [TestMethod]
    public void IsLastWeekdayOfMonth_DetectsLastOccurrence()
    {
        Assert.IsTrue(EventRecurrence.IsLastWeekdayOfMonth(new DateOnly(2026, 10, 27)));
        Assert.IsFalse(EventRecurrence.IsLastWeekdayOfMonth(new DateOnly(2026, 10, 20)));
    }

    #endregion
}
