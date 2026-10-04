using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Controllers;

/// <summary>
/// Checks the Resource Guide still loads when a category has been deleted
/// </summary>
[TestClass]
public class ResourcesControllerTests
{
    private ApplicationDbContext _context = null!;
    private ResourcesController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;
        _context = new ApplicationDbContext(options);

        _controller = new ResourcesController(_context, new TelemetryClient(new TelemetryConfiguration()));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _controller.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task ResourceGuide_Get_DeletedCategoryId_ShowsGuideWithoutResults()
    {
        // Act
        IActionResult result = await _controller.ResourceGuide(categoryID: 999);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result, out ViewResult view);
        Assert.IsInstanceOfType<ResourceGuideModel>(view.Model, out ResourceGuideModel model);
        Assert.IsNull(model.Category);
        Assert.IsEmpty(model.Agencies);
    }

    [TestMethod]
    public async Task ResourceGuide_Get_ExistingCategoryId_ShowsAgencies()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        _context.Agency.Add(new Agency { AgencyName = "Tacoma Housing Authority", AgencyCategories = { housing } });
        await _context.SaveChangesAsync();

        // Act
        IActionResult result = await _controller.ResourceGuide(housing.AgencyCategoryId);

        // Assert
        Assert.IsInstanceOfType<ResourceGuideModel>(((ViewResult)result).Model, out ResourceGuideModel model);
        Assert.AreEqual("Housing", model.Category!.AgencyCategoryName);
        Assert.HasCount(1, model.Agencies);
    }

    [TestMethod]
    public async Task ResourceGuide_Post_UnknownCategoryName_ShowsNoResults()
    {
        // Act
        IActionResult result = await _controller.ResourceGuide(new ResourceGuideModel
        {
            UserSearchedByCityOrService = "true",
            SearchedCategory = "Deleted Category"
        });

        // Assert
        Assert.IsInstanceOfType<ResourceGuideModel>(((ViewResult)result).Model, out ResourceGuideModel model);
        Assert.IsNull(model.Category);
        Assert.IsEmpty(model.Agencies);
    }
}
