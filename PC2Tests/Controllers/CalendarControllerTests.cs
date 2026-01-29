using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
}
