using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;
using System.Security.Claims;

namespace PC2Tests.Controllers;

[TestClass]
public class CalendarControllerTests
{
    private ApplicationDbContext _context = null!;
    private CalendarController _controller = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create a new in-memory database for each test
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;

        _context = new ApplicationDbContext(_options);
        _controller = new CalendarController(_context);

        // Setup mock user with Admin role for authorization
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "testuser@test.com"),
            new Claim(ClaimTypes.Role, IdentityHelper.Admin)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _controller.Dispose();
    }

    #region Index Tests

    [TestMethod]
    public async Task Index_WithNoEvents_ReturnsEmptyViewModel()
    {
        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(0, model.Count);
    }

    [TestMethod]
    public async Task Index_WithSingleEvent_ReturnsSanitizedDescription()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Meeting at test@example.com",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.Count);
        
        var viewModel = model[0];
        Assert.IsNotNull(viewModel.SanitizedDescription);
        // Verify TextLinkifier was called - email should be converted to mailto link
        Assert.IsTrue(viewModel.SanitizedDescription.Contains("mailto:test@example.com"));
        Assert.IsTrue(viewModel.SanitizedDescription.Contains("<a href="));
    }

    [TestMethod]
    public async Task Index_SanitizesHtmlCharacters()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "<script>alert('XSS')</script> Test Event",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.Count);

        var sanitizedDesc = model[0].SanitizedDescription;
        // Script tags should be HTML-encoded
        Assert.IsFalse(sanitizedDesc.Contains("<script>"));
        Assert.IsTrue(sanitizedDesc.Contains("&lt;script&gt;"));
        Assert.IsTrue(sanitizedDesc.Contains("&lt;/script&gt;"));
    }

    [TestMethod]
    public async Task Index_LinkifiesUrls()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Visit https://example.com for more information",
            PC2Event = false,
            CountyEvent = true
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);

        var sanitizedDesc = model[0].SanitizedDescription;
        Assert.IsTrue(sanitizedDesc.Contains("https://example.com"));
        Assert.IsTrue(sanitizedDesc.Contains("<a href=\"https://example.com\""));
        Assert.IsTrue(sanitizedDesc.Contains("target=\"_blank\""));
        Assert.IsTrue(sanitizedDesc.Contains("rel=\"noopener noreferrer\""));
    }

    [TestMethod]
    public async Task Index_LinkifiesPhoneNumbers()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(14, 0),
            EndingTime = new TimeOnly(15, 30),
            EventDescription = "Call 123-456-7890 for registration",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);

        var sanitizedDesc = model[0].SanitizedDescription;
        Assert.IsTrue(sanitizedDesc.Contains("tel:123-456-7890"));
        Assert.IsTrue(sanitizedDesc.Contains("<a href="));
    }

    [TestMethod]
    public async Task Index_LinkifiesMultipleTypes()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            StartingTime = new TimeOnly(9, 0),
            EndingTime = new TimeOnly(10, 0),
            EventDescription = "Contact admin@site.com, visit https://site.com or call 555-123-4567",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);

        var sanitizedDesc = model[0].SanitizedDescription;
        // Check for email link
        Assert.IsTrue(sanitizedDesc.Contains("mailto:admin@site.com"));
        // Check for URL link
        Assert.IsTrue(sanitizedDesc.Contains("https://site.com"));
        // Check for phone link
        Assert.IsTrue(sanitizedDesc.Contains("tel:555-123-4567"));
    }

    [TestMethod]
    public async Task Index_WithMultipleEvents_SanitizesAll()
    {
        // Arrange
        var event1 = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Email: test@example.com",
            PC2Event = true,
            CountyEvent = false
        };
        var event2 = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            StartingTime = new TimeOnly(14, 0),
            EndingTime = new TimeOnly(15, 0),
            EventDescription = "Visit https://example.com",
            PC2Event = false,
            CountyEvent = true
        };
        var event3 = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            StartingTime = new TimeOnly(9, 0),
            EndingTime = new TimeOnly(10, 30),
            EventDescription = "<b>Bold text</b> and call 123-456-7890",
            PC2Event = true,
            CountyEvent = false
        };

        _context.CalendarEvents.AddRange(event1, event2, event3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(3, model.Count);

        // Verify each event is sanitized
        Assert.IsTrue(model[0].SanitizedDescription.Contains("mailto:test@example.com"));
        Assert.IsTrue(model[1].SanitizedDescription.Contains("https://example.com"));
        Assert.IsTrue(model[2].SanitizedDescription.Contains("tel:123-456-7890"));
        // HTML tags should be encoded
        Assert.IsFalse(model[2].SanitizedDescription.Contains("<b>Bold"));
        Assert.IsTrue(model[2].SanitizedDescription.Contains("&lt;b&gt;"));
    }

    [TestMethod]
    public async Task Index_PreservesOriginalEventData()
    {
        // Arrange
        var testDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));
        var startTime = new TimeOnly(13, 30);
        var endTime = new TimeOnly(15, 0);

        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = testDate,
            StartingTime = startTime,
            EndingTime = endTime,
            EventDescription = "Test Event Description",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.Count);

        var viewModel = model[0];
        Assert.IsNotNull(viewModel.Event);
        Assert.AreEqual(testDate, viewModel.Event.DateOfEvent);
        Assert.AreEqual(startTime, viewModel.Event.StartingTime);
        Assert.AreEqual(endTime, viewModel.Event.EndingTime);
        Assert.AreEqual("Test Event Description", viewModel.Event.EventDescription);
        Assert.IsTrue(viewModel.Event.PC2Event);
        Assert.IsFalse(viewModel.Event.CountyEvent);
    }

    [TestMethod]
    public async Task Index_AlwaysCallsTextLinkifier()
    {
        // Arrange - Use a description that TextLinkifier will transform in a specific way
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Contact: email@test.com & visit http://test.com",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);

        var sanitizedDesc = model[0].SanitizedDescription;
        
        // Verify TextLinkifier was called by checking for:
        // 1. HTML encoding of & to &amp;
        // 2. Email converted to mailto link
        // 3. URL converted to anchor tag
        Assert.IsTrue(sanitizedDesc.Contains("&amp;"));
        Assert.IsTrue(sanitizedDesc.Contains("mailto:email@test.com"));
        Assert.IsTrue(sanitizedDesc.Contains("http://test.com"));
    }

    [TestMethod]
    public async Task Index_WithSpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Price: $50 < $100 & > $25. \"Quote\" and Email: test@example.com",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);

        var sanitizedDesc = model[0].SanitizedDescription;
        
        // Special characters should be HTML-encoded
        Assert.IsTrue(sanitizedDesc.Contains("&lt;"));
        Assert.IsTrue(sanitizedDesc.Contains("&gt;"));
        Assert.IsTrue(sanitizedDesc.Contains("&amp;"));
        Assert.IsTrue(sanitizedDesc.Contains("&quot;"));
        // But email should still be linkified
        Assert.IsTrue(sanitizedDesc.Contains("mailto:test@example.com"));
    }

    [TestMethod]
    public async Task Index_DeletesPastEvents()
    {
        // Arrange
        var pastEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Past Event",
            PC2Event = true,
            CountyEvent = false
        };
        var futureEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            StartingTime = new TimeOnly(14, 0),
            EndingTime = new TimeOnly(15, 0),
            EventDescription = "Future Event",
            PC2Event = false,
            CountyEvent = true
        };

        _context.CalendarEvents.AddRange(pastEvent, futureEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        
        // Should only have the future event
        Assert.AreEqual(1, model.Count);
        Assert.AreEqual("Future Event", model[0].Event.EventDescription);
    }

    [TestMethod]
    public async Task Index_EachEventHasBothOriginalAndSanitized()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Visit https://example.com",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.Count);

        var viewModel = model[0];
        // Check original event is preserved
        Assert.IsNotNull(viewModel.Event);
        Assert.AreEqual("Visit https://example.com", viewModel.Event.EventDescription);
        
        // Check sanitized description contains links
        Assert.IsNotNull(viewModel.SanitizedDescription);
        Assert.IsTrue(viewModel.SanitizedDescription.Contains("<a href="));
        Assert.IsTrue(viewModel.SanitizedDescription.Contains("https://example.com"));
    }

    [TestMethod]
    public async Task Index_EmptyDescription_ReturnsSafeEmptyString()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = string.Empty,
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<CalendarEventViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.Count);

        var viewModel = model[0];
        Assert.IsNotNull(viewModel.SanitizedDescription);
        Assert.AreEqual(string.Empty, viewModel.SanitizedDescription);
    }

    #endregion

    #region Edit Tests

    [TestMethod]
    public async Task Edit_EventInSeries_UpdatesEventAndKeepsItInSeries()
    {
        // Arrange
        DateOnly date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var series = new EventSeries
        {
            Frequency = RecurrenceFrequency.Weekly,
            StartDate = date,
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Support group",
            PC2Event = true
        };
        await EventSeriesDB.AddSeries(_context, series, new[] { date, date.AddDays(7) });
        CalendarEvent calendarEvent = series.Events[0];
        _context.ChangeTracker.Clear();

        var model = new CalendarCreateEventViewModel
        {
            EventId = calendarEvent.CalendarEventID,
            DateOfEvent = date.ToDateTime(TimeOnly.MinValue),
            StartingTime = "13:00",
            EndingTime = "14:00",
            Description = "Support group (moved to afternoon)",
            IsPc2Event = true
        };

        // Act
        var result = await _controller.Edit(model) as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);

        CalendarEvent? updated = await _context.CalendarEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CalendarEventID == calendarEvent.CalendarEventID);
        Assert.IsNotNull(updated);
        Assert.AreEqual(series.EventSeriesID, updated.EventSeriesID);
        Assert.AreEqual(new TimeOnly(13, 0), updated.StartingTime);
        Assert.AreEqual("Support group (moved to afternoon)", updated.EventDescription);
    }

    [TestMethod]
    public async Task Edit_MissingEvent_ReturnsNotFound()
    {
        // Arrange
        var model = new CalendarCreateEventViewModel
        {
            EventId = 999,
            DateOfEvent = DateTime.Today.AddDays(1),
            StartingTime = "10:00",
            EndingTime = "11:00",
            Description = "Missing",
            IsPc2Event = true
        };

        // Act
        var result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Edit_Get_EventInSeries_IncludesSeriesSchedule()
    {
        // Arrange
        EventSeries series = await AddWeeklySeries(NextWeekday(DayOfWeek.Monday), 3);

        // Act
        var result = await _controller.Edit(series.Events[0].CalendarEventID) as ViewResult;

        // Assert
        var model = result?.Model as CalendarCreateEventViewModel;
        Assert.IsNotNull(model);
        Assert.AreEqual(series.EventSeriesID, model.SeriesId);
        Assert.AreEqual("Weekly on Monday, 3 times", model.SeriesSchedule);
    }

    #endregion

    #region Repeating event helpers

    /// <summary>
    /// Gets the date of the given weekday at least a week from today
    /// </summary>
    private static DateOnly NextWeekday(DayOfWeek day)
    {
        DateOnly date = DateOnly.FromDateTime(DateTime.Today).AddDays(7);
        while (date.DayOfWeek != day)
        {
            date = date.AddDays(1);
        }
        return date;
    }

    private static CalendarCreateEventViewModel CreateRepeatingModel(DateOnly startDate, RecurrenceInputModel recurrence)
    {
        return new CalendarCreateEventViewModel
        {
            DateOfEvent = startDate.ToDateTime(TimeOnly.MinValue),
            StartingTime = "10:00",
            EndingTime = "11:00",
            Description = "Support group",
            IsPc2Event = true,
            Recurrence = recurrence
        };
    }

    private async Task<EventSeries> AddWeeklySeries(DateOnly startDate, int count)
    {
        var series = new EventSeries
        {
            Frequency = RecurrenceFrequency.Weekly,
            WeeklyDays = (RecurrenceDays)(1 << (int)startDate.DayOfWeek),
            StartDate = startDate,
            OccurrenceCount = count,
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Support group",
            PC2Event = true
        };
        await EventSeriesDB.AddSeries(_context, series,
            EventRecurrence.GetDates(series, startDate, DateOnly.MaxValue));
        return series;
    }

    #endregion

    #region Create Tests

    [TestMethod]
    public async Task Create_NotRepeating_CreatesOneEventWithoutSeries()
    {
        // Arrange
        var model = CreateRepeatingModel(NextWeekday(DayOfWeek.Monday), new RecurrenceInputModel());

        // Act
        var result = await _controller.Create(model) as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        CalendarEvent created = await _context.CalendarEvents.SingleAsync();
        Assert.IsNull(created.EventSeriesID);
        Assert.AreEqual(0, await _context.EventSeries.CountAsync());
    }

    [TestMethod]
    public async Task Create_WeeklyWithCount_CreatesSeriesWithEachDate()
    {
        // Arrange
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        var model = CreateRepeatingModel(start, new RecurrenceInputModel
        {
            Repeat = RepeatOption.Weekly,
            WeeklyDays = { DayOfWeek.Monday, DayOfWeek.Wednesday },
            End = RepeatEnd.AfterCount,
            OccurrenceCount = 4
        });

        // Act
        var result = await _controller.Create(model) as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);

        EventSeries series = await _context.EventSeries.Include(s => s.Events).SingleAsync();
        CollectionAssert.AreEqual(
            new[] { start, start.AddDays(2), start.AddDays(7), start.AddDays(9) },
            series.Events.Select(e => e.DateOfEvent).Order().ToArray());
        Assert.IsTrue(series.Events.All(e => e.EventDescription == "Support group" && e.PC2Event));
        Assert.AreEqual(4, series.OccurrenceCount);
        Assert.IsNull(series.GeneratedThrough);
        Assert.AreEqual("Created a repeating event with 4 dates.", _controller.TempData["CalendarMessage"]);
    }

    [TestMethod]
    public async Task Create_WithExcludedDates_LeavesThemOut()
    {
        // Arrange
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        var model = CreateRepeatingModel(start, new RecurrenceInputModel
        {
            Repeat = RepeatOption.Weekly,
            WeeklyDays = { DayOfWeek.Monday },
            End = RepeatEnd.AfterCount,
            OccurrenceCount = 3,
            ExcludedDates = { start.AddDays(7).ToDateTime(TimeOnly.MinValue) }
        });

        // Act
        await _controller.Create(model);

        // Assert
        List<DateOnly> dates = await _context.CalendarEvents.Select(e => e.DateOfEvent).OrderBy(d => d).ToListAsync();
        CollectionAssert.AreEqual(new[] { start, start.AddDays(14) }, dates);
    }

    [TestMethod]
    public async Task Create_NeverEnding_CreatesAYearOfDates()
    {
        // Arrange
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        var model = CreateRepeatingModel(start, new RecurrenceInputModel { Repeat = RepeatOption.MonthlyByDate });

        // Act
        await _controller.Create(model);

        // Assert
        EventSeries series = await _context.EventSeries.Include(s => s.Events).SingleAsync();
        DateOnly horizon = today.AddMonths(EventRecurrence.OpenEndedHorizonMonths);
        Assert.AreEqual(horizon, series.GeneratedThrough);
        Assert.IsTrue(series.Events.Count >= 11);
        Assert.IsTrue(series.Events.All(e => e.DateOfEvent <= horizon));
    }

    [TestMethod]
    public async Task Create_SpecificDates_CreatesStartDateAndAddedDates()
    {
        // Arrange
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        var model = CreateRepeatingModel(start, new RecurrenceInputModel
        {
            Repeat = RepeatOption.SpecificDates,
            SpecificDates = { start.AddDays(3).ToDateTime(TimeOnly.MinValue), start.AddDays(17).ToDateTime(TimeOnly.MinValue) }
        });

        // Act
        await _controller.Create(model);

        // Assert
        EventSeries series = await _context.EventSeries.Include(s => s.Events).SingleAsync();
        Assert.AreEqual(RecurrenceFrequency.SpecificDates, series.Frequency);
        CollectionAssert.AreEqual(
            new[] { start, start.AddDays(3), start.AddDays(17) },
            series.Events.Select(e => e.DateOfEvent).Order().ToArray());
    }

    [TestMethod]
    public async Task Create_EveryDateExcluded_ReturnsFormWithError()
    {
        // Arrange
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        var model = CreateRepeatingModel(start, new RecurrenceInputModel
        {
            Repeat = RepeatOption.Weekly,
            WeeklyDays = { DayOfWeek.Monday },
            End = RepeatEnd.AfterCount,
            OccurrenceCount = 1,
            ExcludedDates = { start.ToDateTime(TimeOnly.MinValue) }
        });

        // Act
        var result = await _controller.Create(model) as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
        Assert.AreEqual(0, await _context.CalendarEvents.CountAsync());
    }

    #endregion

    #region PreviewDates Tests

    [TestMethod]
    public void PreviewDates_ReturnsDatesSummaryAndExcludedFlags()
    {
        // Arrange
        DateOnly start = NextWeekday(DayOfWeek.Tuesday);
        var recurrence = new RecurrenceInputModel
        {
            Repeat = RepeatOption.Weekly,
            WeeklyDays = { DayOfWeek.Tuesday },
            End = RepeatEnd.AfterCount,
            OccurrenceCount = 3,
            ExcludedDates = { start.AddDays(7).ToDateTime(TimeOnly.MinValue) }
        };

        // Act
        var result = _controller.PreviewDates(start.ToDateTime(TimeOnly.MinValue), recurrence) as JsonResult;

        // Assert
        var preview = result?.Value as RecurrencePreview;
        Assert.IsNotNull(preview);
        Assert.AreEqual("Weekly on Tuesday, 3 times", preview.Summary);
        Assert.HasCount(3, preview.Dates);
        Assert.AreEqual(start.ToString("yyyy-MM-dd"), preview.Dates[0].Date);
        CollectionAssert.AreEqual(new[] { false, true, false }, preview.Dates.Select(d => d.Excluded).ToArray());
        Assert.IsEmpty(preview.Errors);
    }

    [TestMethod]
    public void PreviewDates_InvalidSettings_ReturnsErrorsAndNoDates()
    {
        // Arrange - weekly with no days chosen
        var recurrence = new RecurrenceInputModel { Repeat = RepeatOption.Weekly };

        // Act
        var result = _controller.PreviewDates(NextWeekday(DayOfWeek.Tuesday).ToDateTime(TimeOnly.MinValue), recurrence) as JsonResult;

        // Assert
        var preview = result?.Value as RecurrencePreview;
        Assert.IsNotNull(preview);
        Assert.IsEmpty(preview.Dates);
        CollectionAssert.Contains(preview.Errors.ToList(), "Choose at least one day of the week");
    }

    [TestMethod]
    public void PreviewDates_WithoutStartDate_AsksForDate()
    {
        var result = _controller.PreviewDates(null, new RecurrenceInputModel { Repeat = RepeatOption.MonthlyByDate }) as JsonResult;

        var preview = result?.Value as RecurrencePreview;
        Assert.IsNotNull(preview);
        Assert.HasCount(1, preview.Errors);
    }

    #endregion

    #region EditSeries Tests

    [TestMethod]
    public async Task EditSeries_Get_FillsFormAndMarksDeletedDatesExcluded()
    {
        // Arrange - the second date was deleted
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        EventSeries series = await AddWeeklySeries(start, 4);
        await CalendarEventDB.DeleteEvent(_context, series.Events[1].CalendarEventID);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _controller.EditSeries(series.EventSeriesID) as ViewResult;

        // Assert
        var model = result?.Model as EditSeriesViewModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("Support group", model.Description);
        Assert.AreEqual("10:00", model.StartingTime);
        Assert.AreEqual(start.ToDateTime(TimeOnly.MinValue), model.StartDate);
        Assert.AreEqual(RepeatOption.Weekly, model.Recurrence.Repeat);
        CollectionAssert.AreEqual(new[] { DayOfWeek.Monday }, model.Recurrence.WeeklyDays);
        Assert.AreEqual(RepeatEnd.AfterCount, model.Recurrence.End);
        Assert.AreEqual(4, model.Recurrence.OccurrenceCount);
        CollectionAssert.AreEqual(new[] { start.AddDays(7).ToDateTime(TimeOnly.MinValue) }, model.Recurrence.ExcludedDates);
        Assert.AreEqual(3, model.UpcomingCount);
    }

    [TestMethod]
    public async Task EditSeries_DetailsOnly_UpdatesEveryDateAndKeepsDates()
    {
        // Arrange - one date was moved a day later
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        EventSeries series = await AddWeeklySeries(start, 3);
        series.Events[1].DateOfEvent = start.AddDays(8);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var model = new EditSeriesViewModel
        {
            SeriesId = series.EventSeriesID,
            StartingTime = "13:00",
            EndingTime = "14:30",
            Description = "Support group (new room)",
            IsCountyEvent = true
        };

        // Act
        var result = await _controller.EditSeries(model) as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        List<CalendarEvent> events = await _context.CalendarEvents.OrderBy(e => e.DateOfEvent).ToListAsync();
        CollectionAssert.AreEqual(
            new[] { start, start.AddDays(8), start.AddDays(14) },
            events.Select(e => e.DateOfEvent).ToArray());
        Assert.IsTrue(events.All(e => e.EventDescription == "Support group (new room)"
                                   && e.StartingTime == new TimeOnly(13, 0)
                                   && e.CountyEvent && !e.PC2Event));
    }

    [TestMethod]
    public async Task EditSeries_ChangeSchedule_ReplacesUpcomingDates()
    {
        // Arrange - Mondays become Wednesdays
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        EventSeries series = await AddWeeklySeries(start, 3);
        int keptEventId = series.Events[0].CalendarEventID;
        _context.ChangeTracker.Clear();

        var model = new EditSeriesViewModel
        {
            SeriesId = series.EventSeriesID,
            StartingTime = "10:00",
            EndingTime = "11:00",
            Description = "Support group",
            IsPc2Event = true,
            ChangeSchedule = true,
            StartDate = start.ToDateTime(TimeOnly.MinValue),
            Recurrence = new RecurrenceInputModel
            {
                Repeat = RepeatOption.Weekly,
                WeeklyDays = { DayOfWeek.Monday, DayOfWeek.Wednesday },
                End = RepeatEnd.OnDate,
                EndDate = start.AddDays(9).ToDateTime(TimeOnly.MinValue)
            }
        };

        // Act
        await _controller.EditSeries(model);

        // Assert
        List<CalendarEvent> events = await _context.CalendarEvents.OrderBy(e => e.DateOfEvent).ToListAsync();
        CollectionAssert.AreEqual(
            new[] { start, start.AddDays(2), start.AddDays(7), start.AddDays(9) },
            events.Select(e => e.DateOfEvent).ToArray());
        Assert.AreEqual(keptEventId, events[0].CalendarEventID, "Dates that are kept should not be recreated");

        EventSeries updated = await _context.EventSeries.SingleAsync();
        Assert.AreEqual(start.AddDays(9), updated.EndDate);
        Assert.IsNull(updated.OccurrenceCount);
    }

    [TestMethod]
    public async Task EditSeries_MissingSeries_ReturnsNotFound()
    {
        Assert.IsInstanceOfType<NotFoundResult>(await _controller.EditSeries(999));
    }

    #endregion

    #region DeleteSeries Tests

    [TestMethod]
    public async Task DeleteSeries_Get_ListsUpcomingDates()
    {
        // Arrange
        DateOnly start = NextWeekday(DayOfWeek.Monday);
        EventSeries series = await AddWeeklySeries(start, 2);

        // Act
        var result = await _controller.DeleteSeries(series.EventSeriesID) as ViewResult;

        // Assert
        var model = result?.Model as DeleteSeriesViewModel;
        Assert.IsNotNull(model);
        CollectionAssert.AreEqual(new[] { start, start.AddDays(7) }, model.UpcomingDates);
        Assert.AreEqual("Weekly on Monday, 2 times", model.Schedule);
    }

    [TestMethod]
    public async Task DeleteSeriesConfirmed_RemovesSeriesAndDates()
    {
        // Arrange
        EventSeries series = await AddWeeklySeries(NextWeekday(DayOfWeek.Monday), 3);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _controller.DeleteSeriesConfirmed(series.EventSeriesID) as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, await _context.EventSeries.CountAsync());
        Assert.AreEqual(0, await _context.CalendarEvents.CountAsync());
    }

    #endregion
}
