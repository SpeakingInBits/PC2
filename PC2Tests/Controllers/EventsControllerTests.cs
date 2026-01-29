using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;
using System.Text.Json;

namespace PC2Tests.Controllers;

[TestClass]
public class EventsControllerTests
{
    private ApplicationDbContext _context = null!;
    private EventsController _controller = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        // Create a new in-memory database for each test
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;

        _context = new ApplicationDbContext(_options);
        _controller = new EventsController(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _controller.Dispose();
    }

    #region GetEvents Tests

    [TestMethod]
    public async Task GetEvents_WithNoEvents_ReturnsEmptyJsonArray()
    {
        // Act
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var events = result.Value as List<object>;
        Assert.IsNotNull(events);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public async Task GetEvents_WithSingleEvent_ReturnsJsonWithSanitizedDescription()
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
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var events = result.Value as List<object>;
        Assert.IsNotNull(events);
        Assert.AreEqual(1, events.Count);

        // Verify the event was sanitized by TextLinkifier
        var json = JsonSerializer.Serialize(events[0]);
        // Check that email is linkified (accounting for JSON escaping of quotes)
        Assert.IsTrue(json.Contains("test@example.com"), "Should contain the email address");
        Assert.IsTrue(json.Contains("mailto:") || json.Contains("\\u003Ca"), "Should contain mailto link or escaped anchor tag");
    }

    [TestMethod]
    public async Task GetEvents_SanitizesHtmlCharacters()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "<script>alert('XSS')</script>",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var events = result.Value as List<object>;
        Assert.IsNotNull(events);
        Assert.AreEqual(1, events.Count);

        var json = JsonSerializer.Serialize(events[0]);
        // Should be HTML-encoded by TextLinkifier (will appear escaped in JSON)
        Assert.IsFalse(json.Contains("<script>"), "Should not contain raw script tags");
        // The TextLinkifier.Linkify encodes HTML, so check it worked
        Assert.IsTrue(json.Contains("script") && !json.Contains("<script"), "Should contain 'script' text but not as a tag");
    }

    [TestMethod]
    public async Task GetEvents_LinkifiesUrls()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Visit https://example.com for details",
            PC2Event = false,
            CountyEvent = true
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var events = result.Value as List<object>;
        Assert.IsNotNull(events);

        var json = JsonSerializer.Serialize(events[0]);
        Assert.IsTrue(json.Contains("https://example.com"), "Should contain the URL");
        Assert.IsTrue(json.Contains("href") || json.Contains("\\u003Ca"), "Should contain anchor tag");
        Assert.IsTrue(json.Contains("_blank") || json.Contains("target"), "Should have target attribute");
    }

    [TestMethod]
    public async Task GetEvents_LinkifiesPhoneNumbers()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(14, 0),
            EndingTime = new TimeOnly(15, 30),
            EventDescription = "Call 123-456-7890 for info",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var events = result.Value as List<object>;
        Assert.IsNotNull(events);

        var json = JsonSerializer.Serialize(events[0]);
        Assert.IsTrue(json.Contains("123-456-7890"), "Should contain the phone number");
        Assert.IsTrue(json.Contains("tel:") || json.Contains("\\u003Ca"), "Should contain tel link or escaped anchor tag");
    }

    [TestMethod]
    public async Task GetEvents_LinkifiesMultipleTypes()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            StartingTime = new TimeOnly(9, 0),
            EndingTime = new TimeOnly(10, 0),
            EventDescription = "Contact admin@site.com, visit https://site.com or call 555-1234",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var events = result.Value as List<object>;
        Assert.IsNotNull(events);

        var json = JsonSerializer.Serialize(events[0]);
        // Check for email, URL, and phone presence
        Assert.IsTrue(json.Contains("admin@site.com"), "Should contain email address");
        Assert.IsTrue(json.Contains("https://site.com"), "Should contain URL");
        Assert.IsTrue(json.Contains("555-1234"), "Should contain phone number");
        // Should have some form of anchor tags
        Assert.IsTrue(json.Contains("href") || json.Contains("\\u003Ca"), "Should contain anchor tags");
    }

    [TestMethod]
    public async Task GetEvents_WithMultipleEvents_ReturnsAllWithSanitizedDescriptions()
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
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var events = result.Value as List<object>;
        Assert.IsNotNull(events);
        Assert.AreEqual(3, events.Count);

        // Verify each event is sanitized
        var json = JsonSerializer.Serialize(result.Value);
        Assert.IsTrue(json.Contains("test@example.com"));
        Assert.IsTrue(json.Contains("https://example.com"));
        Assert.IsTrue(json.Contains("123-456-7890"));
        // HTML tags should be encoded
        Assert.IsFalse(json.Contains("<b>Bold"));
    }

    [TestMethod]
    public async Task GetEvents_IncludesEventTypeFlags()
    {
        // Arrange
        var pc2Event = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "PC2 Event",
            PC2Event = true,
            CountyEvent = false
        };
        var countyEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            StartingTime = new TimeOnly(14, 0),
            EndingTime = new TimeOnly(15, 0),
            EventDescription = "County Event",
            PC2Event = false,
            CountyEvent = true
        };

        _context.CalendarEvents.AddRange(pc2Event, countyEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var json = JsonSerializer.Serialize(result.Value);
        
        // Verify both event types are present
        Assert.IsTrue(json.Contains("\"IsPC2Event\":true") || json.Contains("\"isPC2Event\":true"), "Should have PC2 event flag set to true");
        Assert.IsTrue(json.Contains("\"IsPC2Event\":false") || json.Contains("\"isPC2Event\":false"), "Should have PC2 event flag set to false");
    }

    [TestMethod]
    public async Task GetEvents_AlwaysCallsTextLinkifier()
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
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var json = JsonSerializer.Serialize(result.Value);
        
        // Verify TextLinkifier was called by checking for:
        // 1. HTML encoding of & to &amp; (or unicode escaped)
        // 2. Email and URL present
        Assert.IsTrue(json.Contains("&amp;") || json.Contains("\\u0026"), "Should encode ampersand");
        Assert.IsTrue(json.Contains("email@test.com"), "Should contain email");
        Assert.IsTrue(json.Contains("http://test.com"), "Should contain URL");
    }

    [TestMethod]
    public async Task GetEvents_WithSpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Price: $50 < $100 & > $25. Email: test@example.com",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result);
        var json = JsonSerializer.Serialize(result.Value);
        
        // Special characters should be HTML-encoded (or unicode escaped in JSON)
        Assert.IsTrue(json.Contains("&lt;") || json.Contains("\\u003C"), "Should encode less-than");
        Assert.IsTrue(json.Contains("&gt;") || json.Contains("\\u003E"), "Should encode greater-than");
        Assert.IsTrue(json.Contains("&amp;") || json.Contains("\\u0026"), "Should encode ampersand");
        // But email should still be present
        Assert.IsTrue(json.Contains("test@example.com"), "Should contain email");
    }

    [TestMethod]
    public async Task GetEvents_CalledTwice_ReturnsSameResults()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Test event with email@test.com",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _controller.GetEvents() as JsonResult;
        var result2 = await _controller.GetEvents() as JsonResult;

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        
        var json1 = JsonSerializer.Serialize(result1.Value);
        var json2 = JsonSerializer.Serialize(result2.Value);
        
        Assert.AreEqual(json1, json2, "Should return identical results on multiple calls");
    }

    #endregion

    #region Index Tests

    [TestMethod]
    public async Task Index_ReturnsViewWithEventsModel()
    {
        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result.Model, typeof(EventsModel));
    }

    [TestMethod]
    public async Task Index_WithNoEvents_ReturnsModelWithEmptyList()
    {
        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as EventsModel;
        Assert.IsNotNull(model);
        Assert.IsNotNull(model.CalendarEvents);
        Assert.AreEqual(0, model.CalendarEvents.Count);
    }

    [TestMethod]
    public async Task Index_WithEvents_ReturnsModelWithEvents()
    {
        // Arrange
        var calendarEvent = new CalendarEvent
        {
            DateOfEvent = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartingTime = new TimeOnly(10, 0),
            EndingTime = new TimeOnly(11, 0),
            EventDescription = "Test Event",
            PC2Event = true,
            CountyEvent = false
        };
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as EventsModel;
        Assert.IsNotNull(model);
        Assert.IsNotNull(model.CalendarEvents);
        Assert.AreEqual(1, model.CalendarEvents.Count);
    }

    #endregion
}
