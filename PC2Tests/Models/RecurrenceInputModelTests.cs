using System.ComponentModel.DataAnnotations;
using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class RecurrenceInputModelTests
{
    // Tuesday, October 13, 2026 is the second Tuesday of the month
    private static readonly DateOnly Start = new(2026, 10, 13);
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static List<ValidationResult> Validate(RecurrenceInputModel model, DateOnly? startDate = null)
    {
        return model.Validate(startDate ?? Start, Today, "Recurrence").ToList();
    }

    private static void AssertSingleError(List<ValidationResult> results, string field)
    {
        Assert.HasCount(1, results);
        CollectionAssert.AreEqual(new[] { $"Recurrence.{field}" }, results[0].MemberNames.ToArray());
    }

    #region Validate

    [TestMethod]
    public void Validate_NotRepeating_HasNoErrors()
    {
        // Other fields are ignored when the event doesn't repeat
        var model = new RecurrenceInputModel { Interval = 0, End = RepeatEnd.OnDate };

        Assert.IsEmpty(Validate(model));
    }

    [TestMethod]
    public void Validate_WeeklyWithDay_HasNoErrors()
    {
        var model = new RecurrenceInputModel { Repeat = RepeatOption.Weekly, WeeklyDays = { DayOfWeek.Tuesday } };

        Assert.IsEmpty(Validate(model));
    }

    [TestMethod]
    public void Validate_WeeklyWithoutDays_RequiresADay()
    {
        var model = new RecurrenceInputModel { Repeat = RepeatOption.Weekly };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.WeeklyDays));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(13)]
    public void Validate_IntervalOutOfRange_IsAnError(int interval)
    {
        var model = new RecurrenceInputModel { Repeat = RepeatOption.MonthlyByDate, Interval = interval };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.Interval));
    }

    [TestMethod]
    public void Validate_LastWeekdayOnDateThatIsNotLast_IsAnError()
    {
        var model = new RecurrenceInputModel { Repeat = RepeatOption.MonthlyByLastWeekday };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.Repeat));
        Assert.IsEmpty(Validate(model, new DateOnly(2026, 10, 27)));
    }

    [TestMethod]
    public void Validate_EndDateMissing_IsAnError()
    {
        var model = new RecurrenceInputModel { Repeat = RepeatOption.MonthlyByDate, End = RepeatEnd.OnDate };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.EndDate));
    }

    [TestMethod]
    public void Validate_EndDateBeforeStart_IsAnError()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.MonthlyByDate,
            End = RepeatEnd.OnDate,
            EndDate = new DateTime(2026, 10, 12)
        };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.EndDate));
    }

    [TestMethod]
    public void Validate_EndDateTooFarAhead_IsAnError()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.MonthlyByDate,
            End = RepeatEnd.OnDate,
            EndDate = new DateTime(2028, 10, 14)
        };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.EndDate));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(0)]
    [DataRow(101)]
    public void Validate_CountOutOfRange_IsAnError(int? count)
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.MonthlyByDate,
            End = RepeatEnd.AfterCount,
            OccurrenceCount = count
        };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.OccurrenceCount));
    }

    [TestMethod]
    public void Validate_SpecificDatesEmpty_RequiresADate()
    {
        var model = new RecurrenceInputModel { Repeat = RepeatOption.SpecificDates };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.SpecificDates));
    }

    [TestMethod]
    public void Validate_SpecificDateInThePast_IsAnError()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.SpecificDates,
            SpecificDates = { new DateTime(2026, 10, 1) }
        };

        AssertSingleError(Validate(model), nameof(RecurrenceInputModel.SpecificDates));
    }

    [TestMethod]
    public void CalendarCreateEventViewModel_WhenEditingSingleDate_SkipsRecurrence()
    {
        var model = new CalendarCreateEventViewModel
        {
            EventId = 5,
            DateOfEvent = DateTime.Today.AddDays(1),
            StartingTime = "10:00",
            EndingTime = "11:00",
            Description = "Event",
            IsPc2Event = true,
            Recurrence = new RecurrenceInputModel { Repeat = RepeatOption.Weekly }
        };

        Assert.IsEmpty(model.Validate(new ValidationContext(model)));
    }

    [TestMethod]
    public void CalendarCreateEventViewModel_WhenCreating_ValidatesRecurrence()
    {
        var model = new CalendarCreateEventViewModel
        {
            DateOfEvent = DateTime.Today.AddDays(1),
            StartingTime = "10:00",
            EndingTime = "11:00",
            Description = "Event",
            IsPc2Event = true,
            Recurrence = new RecurrenceInputModel { Repeat = RepeatOption.Weekly }
        };

        List<ValidationResult> results = model.Validate(new ValidationContext(model)).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Recurrence.WeeklyDays", results[0].MemberNames.Single());
    }

    #endregion

    #region ApplyTo

    [TestMethod]
    public void ApplyTo_Weekly_SetsDaysAndInterval()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.Weekly,
            Interval = 2,
            WeeklyDays = { DayOfWeek.Tuesday, DayOfWeek.Thursday },
            End = RepeatEnd.AfterCount,
            OccurrenceCount = 6
        };
        var series = new EventSeries { EventDescription = "Event" };

        model.ApplyTo(series, Start);

        Assert.AreEqual(RecurrenceFrequency.Weekly, series.Frequency);
        Assert.AreEqual(RecurrenceDays.Tuesday | RecurrenceDays.Thursday, series.WeeklyDays);
        Assert.AreEqual(2, series.Interval);
        Assert.AreEqual(6, series.OccurrenceCount);
        Assert.IsNull(series.EndDate);
        Assert.AreEqual(Start, series.StartDate);
    }

    [TestMethod]
    public void ApplyTo_MonthlyByWeekday_UsesWeekOfStartDate()
    {
        var model = new RecurrenceInputModel { Repeat = RepeatOption.MonthlyByWeekday };
        var series = new EventSeries { EventDescription = "Event" };

        model.ApplyTo(series, Start);

        Assert.AreEqual(RecurrenceFrequency.MonthlyByWeekday, series.Frequency);
        Assert.AreEqual(2, series.WeekOfMonth);
        Assert.IsTrue(series.IsOpenEnded);
    }

    [TestMethod]
    public void ApplyTo_MonthlyByLastWeekday_UsesLastWeek()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.MonthlyByLastWeekday,
            End = RepeatEnd.OnDate,
            EndDate = new DateTime(2027, 3, 1)
        };
        var series = new EventSeries { EventDescription = "Event" };

        model.ApplyTo(series, new DateOnly(2026, 10, 27));

        Assert.AreEqual(EventSeries.LastWeekOfMonth, series.WeekOfMonth);
        Assert.AreEqual(new DateOnly(2027, 3, 1), series.EndDate);
    }

    [TestMethod]
    public void ApplyTo_SpecificDates_IgnoresEndSettings()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.SpecificDates,
            End = RepeatEnd.AfterCount,
            OccurrenceCount = 3,
            Interval = 4
        };
        var series = new EventSeries { EventDescription = "Event" };

        model.ApplyTo(series, Start);

        Assert.AreEqual(RecurrenceFrequency.SpecificDates, series.Frequency);
        Assert.IsNull(series.OccurrenceCount);
        Assert.AreEqual(1, series.Interval);
        Assert.IsFalse(series.IsOpenEnded);
    }

    #endregion

    #region GetSelectedDates

    [TestMethod]
    public void GetSelectedDates_LeavesOutExcludedDates()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.MonthlyByWeekday,
            End = RepeatEnd.AfterCount,
            OccurrenceCount = 3,
            ExcludedDates = { new DateTime(2026, 11, 10) }
        };

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 10, 13), new DateOnly(2026, 12, 8) },
            model.GetSelectedDates(Start, Today));
    }

    [TestMethod]
    public void GetSelectedDates_SpecificDates_IncludesStartDateInOrder()
    {
        var model = new RecurrenceInputModel
        {
            Repeat = RepeatOption.SpecificDates,
            SpecificDates = { new DateTime(2026, 11, 2), new DateTime(2026, 10, 20), new DateTime(2026, 11, 2) }
        };

        CollectionAssert.AreEqual(
            new[] { Start, new DateOnly(2026, 10, 20), new DateOnly(2026, 11, 2) },
            model.GetSelectedDates(Start, Today));
    }

    #endregion

    #region FromSeries

    [TestMethod]
    public void FromSeries_Weekly_RoundTrips()
    {
        var series = new EventSeries
        {
            Frequency = RecurrenceFrequency.Weekly,
            WeeklyDays = RecurrenceDays.Tuesday | RecurrenceDays.Thursday,
            Interval = 2,
            StartDate = Start,
            EndDate = new DateOnly(2027, 1, 31),
            EventDescription = "Event"
        };
        List<DateOnly> upcoming = EventRecurrence.GetDates(series, Start, series.EndDate.Value).ToList();

        RecurrenceInputModel model = RecurrenceInputModel.FromSeries(series, upcoming, Start, Today);

        Assert.AreEqual(RepeatOption.Weekly, model.Repeat);
        CollectionAssert.AreEqual(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, model.WeeklyDays);
        Assert.AreEqual(2, model.Interval);
        Assert.AreEqual(RepeatEnd.OnDate, model.End);
        Assert.AreEqual(new DateTime(2027, 1, 31), model.EndDate);
        Assert.IsEmpty(model.ExcludedDates);
        CollectionAssert.AreEqual(upcoming, model.GetSelectedDates(Start, Today));
    }

    [TestMethod]
    public void FromSeries_CountedSeries_ShowsRemainingCountFromNewStart()
    {
        // Started a month earlier with 6 dates; 5 remain from October 13
        var series = new EventSeries
        {
            Frequency = RecurrenceFrequency.MonthlyByWeekday,
            WeekOfMonth = 2,
            StartDate = new DateOnly(2026, 9, 8),
            OccurrenceCount = 6,
            EventDescription = "Event"
        };
        List<DateOnly> upcoming = EventRecurrence.GetDates(series, Start, DateOnly.MaxValue).ToList();

        RecurrenceInputModel model = RecurrenceInputModel.FromSeries(series, upcoming, Start, Today);

        Assert.AreEqual(RepeatOption.MonthlyByWeekday, model.Repeat);
        Assert.AreEqual(RepeatEnd.AfterCount, model.End);
        Assert.AreEqual(5, model.OccurrenceCount);
        CollectionAssert.AreEqual(upcoming, model.GetSelectedDates(Start, Today));
    }

    [TestMethod]
    public void FromSeries_SpecificDates_ListsDatesAfterStart()
    {
        var series = new EventSeries
        {
            Frequency = RecurrenceFrequency.SpecificDates,
            StartDate = Start,
            EventDescription = "Event"
        };
        DateOnly[] upcoming = { Start, new(2026, 10, 20), new(2026, 11, 2) };

        RecurrenceInputModel model = RecurrenceInputModel.FromSeries(series, upcoming, Start, Today);

        Assert.AreEqual(RepeatOption.SpecificDates, model.Repeat);
        CollectionAssert.AreEqual(
            new[] { new DateTime(2026, 10, 20), new DateTime(2026, 11, 2) },
            model.SpecificDates);
    }

    #endregion
}
