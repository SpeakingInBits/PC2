using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class JobOpportunityDBTests
{
    private ApplicationDbContext _context = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

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

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_WithValidJob_AddsToDatabase()
    {
        // Arrange
        var job = new JobOpportunity
        {
            Title = "Program Coordinator",
            Description = "Coordinates programs for PC2."
        };

        // Act
        await JobOpportunityDB.AddAsync(_context, job);

        // Assert
        var result = await _context.JobOpportunities.ToListAsync();
        Assert.HasCount(1, result);
        Assert.AreEqual("Program Coordinator", result[0].Title);
        Assert.AreEqual("Coordinates programs for PC2.", result[0].Description);
        Assert.IsNull(result[0].ClosingDate);
        Assert.IsFalse(result[0].IsClosed);
    }

    [TestMethod]
    public async Task AddAsync_WithClosingDate_AddsToDatabase()
    {
        // Arrange
        var job = new JobOpportunity
        {
            Title = "Office Assistant",
            Description = "Assists with office duties.",
            ClosingDate = Today.AddDays(30)
        };

        // Act
        await JobOpportunityDB.AddAsync(_context, job);

        // Assert
        var result = await _context.JobOpportunities.FindAsync(job.JobOpportunityId);
        Assert.IsNotNull(result);
        Assert.AreEqual(Today.AddDays(30), result.ClosingDate);
    }

    [TestMethod]
    public async Task AddAsync_WithAttachment_AddsToDatabase()
    {
        // Arrange
        var job = new JobOpportunity
        {
            Title = "Program Coordinator",
            Description = "Coordinates programs for PC2.",
            AttachmentLocation = "https://example.blob.core.windows.net/files/job-description.pdf",
            AttachmentName = "job-description.pdf"
        };

        // Act
        await JobOpportunityDB.AddAsync(_context, job);

        // Assert
        var result = await _context.JobOpportunities.FindAsync(job.JobOpportunityId);
        Assert.IsNotNull(result);
        Assert.AreEqual("https://example.blob.core.windows.net/files/job-description.pdf", result.AttachmentLocation);
        Assert.AreEqual("job-description.pdf", result.AttachmentName);
    }

    #endregion

    #region GetAllAsync Tests

    [TestMethod]
    public async Task GetAllAsync_WithNoJobs_ReturnsEmptyList()
    {
        // Act
        var result = await JobOpportunityDB.GetAllAsync(_context);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsOpenJobsFirst()
    {
        // Arrange
        var closedJob = new JobOpportunity { Title = "A Closed Job", Description = "Closed", IsClosed = true };
        var openJob = new JobOpportunity { Title = "Z Open Job", Description = "Open" };
        _context.JobOpportunities.AddRange(closedJob, openJob);
        await _context.SaveChangesAsync();

        // Act
        var result = await JobOpportunityDB.GetAllAsync(_context);

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual("Z Open Job", result[0].Title);
        Assert.AreEqual("A Closed Job", result[1].Title);
    }

    [TestMethod]
    public async Task GetAllAsync_TreatsPastClosingDateAsClosed()
    {
        // Arrange
        var expiredJob = new JobOpportunity
        {
            Title = "A Expired Job",
            Description = "Closing date has passed",
            ClosingDate = Today.AddDays(-1)
        };
        var openJob = new JobOpportunity { Title = "Z Open Job", Description = "Open" };
        _context.JobOpportunities.AddRange(expiredJob, openJob);
        await _context.SaveChangesAsync();

        // Act
        var result = await JobOpportunityDB.GetAllAsync(_context);

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual("Z Open Job", result[0].Title);
        Assert.AreEqual("A Expired Job", result[1].Title);
    }

    #endregion

    #region GetOpenAsync Tests

    [TestMethod]
    public async Task GetOpenAsync_ExcludesManuallyClosedJobs()
    {
        // Arrange
        var openJob = new JobOpportunity { Title = "Open Job", Description = "Open" };
        var closedJob = new JobOpportunity { Title = "Closed Job", Description = "Closed", IsClosed = true };
        _context.JobOpportunities.AddRange(openJob, closedJob);
        await _context.SaveChangesAsync();

        // Act
        var result = await JobOpportunityDB.GetOpenAsync(_context);

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("Open Job", result[0].Title);
    }

    [TestMethod]
    public async Task GetOpenAsync_ExcludesJobsWithPastClosingDate()
    {
        // Arrange
        var expiredJob = new JobOpportunity
        {
            Title = "Expired Job",
            Description = "Closing date has passed",
            ClosingDate = Today.AddDays(-1)
        };
        var currentJob = new JobOpportunity
        {
            Title = "Current Job",
            Description = "Closing date is in the future",
            ClosingDate = Today.AddDays(14)
        };
        _context.JobOpportunities.AddRange(expiredJob, currentJob);
        await _context.SaveChangesAsync();

        // Act
        var result = await JobOpportunityDB.GetOpenAsync(_context);

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("Current Job", result[0].Title);
    }

    [TestMethod]
    public async Task GetOpenAsync_IncludesJobClosingToday()
    {
        // Arrange
        var job = new JobOpportunity
        {
            Title = "Closing Today",
            Description = "Last day to apply",
            ClosingDate = Today
        };
        _context.JobOpportunities.Add(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await JobOpportunityDB.GetOpenAsync(_context);

        // Assert
        Assert.HasCount(1, result);
    }

    #endregion

    #region GetJobAsync Tests

    [TestMethod]
    public async Task GetJobAsync_WithValidId_ReturnsJob()
    {
        // Arrange
        var job = new JobOpportunity { Title = "Program Coordinator", Description = "Coordinates programs." };
        _context.JobOpportunities.Add(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await JobOpportunityDB.GetJobAsync(_context, job.JobOpportunityId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Program Coordinator", result.Title);
    }

    [TestMethod]
    public async Task GetJobAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await JobOpportunityDB.GetJobAsync(_context, 999);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region UpdateAsync Tests

    [TestMethod]
    public async Task UpdateAsync_WithValidChanges_UpdatesDatabase()
    {
        // Arrange
        var job = new JobOpportunity { Title = "Old Title", Description = "Old description" };
        _context.JobOpportunities.Add(job);
        await _context.SaveChangesAsync();

        _context.Entry(job).State = EntityState.Detached;
        job.Title = "New Title";
        job.Description = "New description";
        job.IsClosed = true;

        // Act
        await JobOpportunityDB.UpdateAsync(_context, job);

        // Assert
        var result = await _context.JobOpportunities.FindAsync(job.JobOpportunityId);
        Assert.IsNotNull(result);
        Assert.AreEqual("New Title", result.Title);
        Assert.AreEqual("New description", result.Description);
        Assert.IsTrue(result.IsClosed);
    }

    #endregion

    #region DeleteAsync Tests

    [TestMethod]
    public async Task DeleteAsync_WithValidId_RemovesFromDatabase()
    {
        // Arrange
        var job = new JobOpportunity { Title = "Program Coordinator", Description = "Coordinates programs." };
        _context.JobOpportunities.Add(job);
        await _context.SaveChangesAsync();
        int id = job.JobOpportunityId;

        // Act
        await JobOpportunityDB.DeleteAsync(_context, id);

        // Assert
        var result = await _context.JobOpportunities.FindAsync(id);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task DeleteAsync_WithInvalidId_DoesNotThrow()
    {
        // Act & Assert - should not throw
        await JobOpportunityDB.DeleteAsync(_context, 999);
    }

    #endregion

    #region IsOpen Tests

    [TestMethod]
    public void IsOpen_WithNoClosingDateAndNotClosed_ReturnsTrue()
    {
        var job = new JobOpportunity { Title = "Job", Description = "Description" };
        Assert.IsTrue(job.IsOpen);
    }

    [TestMethod]
    public void IsOpen_WhenManuallyClosed_ReturnsFalse()
    {
        var job = new JobOpportunity { Title = "Job", Description = "Description", IsClosed = true };
        Assert.IsFalse(job.IsOpen);
    }

    [TestMethod]
    public void IsOpen_WithPastClosingDate_ReturnsFalse()
    {
        var job = new JobOpportunity
        {
            Title = "Job",
            Description = "Description",
            ClosingDate = Today.AddDays(-1)
        };
        Assert.IsFalse(job.IsOpen);
    }

    [TestMethod]
    public void IsOpen_WithFutureClosingDate_ReturnsTrue()
    {
        var job = new JobOpportunity
        {
            Title = "Job",
            Description = "Description",
            ClosingDate = Today.AddDays(1)
        };
        Assert.IsTrue(job.IsOpen);
    }

    #endregion
}
