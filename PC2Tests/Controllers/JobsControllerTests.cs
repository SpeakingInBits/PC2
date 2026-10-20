using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Controllers;

[TestClass]
public class JobsControllerTests
{
    private static readonly DateOnly ClosingDate = new(2026, 10, 22);

    private ApplicationDbContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;
        _context = new ApplicationDbContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private JobsController CreateController(string utcNow)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureBlob:BlobServiceUri"] = "UseDevelopmentStorage=true",
                ["AzureBlob:ContainerName"] = "files"
            })
            .Build();
        var controller = new JobsController(_context, new AzureBlobUploader(configuration),
            new FixedTimeProvider(utcNow), NullLogger<JobsController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private async Task<JobOpportunity> AddClosedJobAsync()
    {
        var job = new JobOpportunity
        {
            Title = "Program Coordinator",
            Description = "Coordinates programs for PC2.",
            ClosingDate = ClosingDate,
            IsClosed = true
        };
        _context.JobOpportunities.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    #region Reopen Tests

    [TestMethod]
    public async Task Reopen_At8PmPacificOnClosingDate_KeepsClosingDate()
    {
        // Arrange - 8 PM PDT on the closing date, when UTC is already the next day
        JobOpportunity job = await AddClosedJobAsync();
        using JobsController controller = CreateController("2026-10-23T03:00:00Z");

        // Act
        await controller.Reopen(job.JobOpportunityId);

        // Assert
        var result = await _context.JobOpportunities.FindAsync(job.JobOpportunityId);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsClosed);
        Assert.AreEqual(ClosingDate, result.ClosingDate);
    }

    [TestMethod]
    public async Task Reopen_AfterMidnightPacific_ClearsPastClosingDate()
    {
        // Arrange - 12:30 AM PDT the day after the closing date
        JobOpportunity job = await AddClosedJobAsync();
        using JobsController controller = CreateController("2026-10-23T07:30:00Z");

        // Act
        await controller.Reopen(job.JobOpportunityId);

        // Assert
        var result = await _context.JobOpportunities.FindAsync(job.JobOpportunityId);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsClosed);
        Assert.IsNull(result.ClosingDate);
    }

    #endregion
}
