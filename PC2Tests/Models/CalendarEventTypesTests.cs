using System.ComponentModel.DataAnnotations;
using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class CalendarEventTypesTests
{
    [TestMethod]
    [DataRow(CalendarEventTypes.Pc2, true, false)]
    [DataRow(CalendarEventTypes.County, false, true)]
    [DataRow(null, false, false)]
    [DataRow("Something else", false, false)]
    public void EventType_Set_SetsFlags(string? eventType, bool expectedPc2, bool expectedCounty)
    {
        var createModel = new CalendarCreateEventViewModel { IsPc2Event = true, IsCountyEvent = true, EventType = eventType };
        var seriesModel = new EditSeriesViewModel { IsPc2Event = true, IsCountyEvent = true, EventType = eventType };

        Assert.AreEqual(expectedPc2, createModel.IsPc2Event);
        Assert.AreEqual(expectedCounty, createModel.IsCountyEvent);
        Assert.AreEqual(expectedPc2, seriesModel.IsPc2Event);
        Assert.AreEqual(expectedCounty, seriesModel.IsCountyEvent);
    }

    [TestMethod]
    [DataRow(true, false, CalendarEventTypes.Pc2)]
    [DataRow(false, true, CalendarEventTypes.County)]
    [DataRow(false, false, null)]
    [DataRow(true, true, null)]
    public void EventType_Get_MatchesFlags(bool isPc2Event, bool isCountyEvent, string? expected)
    {
        var model = new CalendarCreateEventViewModel { IsPc2Event = isPc2Event, IsCountyEvent = isCountyEvent };

        Assert.AreEqual(expected, model.EventType);
    }

    [TestMethod]
    public void Validate_NoEventType_ReportsErrorOnEventType()
    {
        var model = new CalendarCreateEventViewModel
        {
            DateOfEvent = DateTime.Today.AddDays(1),
            StartingTime = "10:00",
            EndingTime = "11:00",
            Description = "Event"
        };

        List<ValidationResult> results = model.Validate(new ValidationContext(model)).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual(CalendarEventTypes.RequiredMessage, results[0].ErrorMessage);
        CollectionAssert.AreEqual(new[] { nameof(CalendarCreateEventViewModel.EventType) }, results[0].MemberNames.ToArray());
    }

    [TestMethod]
    public void Validate_EditSeriesWithEventType_HasNoErrors()
    {
        var model = new EditSeriesViewModel
        {
            StartingTime = "10:00",
            EndingTime = "11:00",
            Description = "Event",
            EventType = CalendarEventTypes.County
        };

        Assert.IsEmpty(model.Validate(new ValidationContext(model)));
    }
}
