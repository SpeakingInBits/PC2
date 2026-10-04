using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Controllers;

[TestClass]
public class AgencyCategoryControllerTests
{
    private ApplicationDbContext _context = null!;
    private AgencyCategoryController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .Options;
        _context = new ApplicationDbContext(options);

        _controller = new AgencyCategoryController(_context);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.TempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _controller.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<AgencyCategory> AddCategoryAsync(string name)
    {
        var category = new AgencyCategory { AgencyCategoryName = name };
        _context.AgencyCategory.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    [TestMethod]
    public async Task Create_NewName_AddsTrimmedCategoryAndRedirects()
    {
        // Act
        IActionResult result = await _controller.Create(new AgencyCategory { AgencyCategoryName = "  Housing  " });

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result, out RedirectToActionResult redirect);
        Assert.AreEqual("Manage", redirect.ActionName);
        AgencyCategory saved = await _context.AgencyCategory.SingleAsync();
        Assert.AreEqual("Housing", saved.AgencyCategoryName);
    }

    [TestMethod]
    public async Task Create_DuplicateName_ReturnsViewWithError()
    {
        // Arrange
        await AddCategoryAsync("Housing");

        // Act
        IActionResult result = await _controller.Create(new AgencyCategory { AgencyCategoryName = "housing" });

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
        Assert.AreEqual(AgencyCategoryController.DuplicateNameMessage,
            _controller.ModelState[nameof(AgencyCategory.AgencyCategoryName)]!.Errors[0].ErrorMessage);
        Assert.AreEqual(1, await _context.AgencyCategory.CountAsync());
    }

    [TestMethod]
    public async Task Edit_NewName_RenamesCategory()
    {
        // Arrange
        AgencyCategory housing = await AddCategoryAsync("Housing");

        // Act
        IActionResult result = await _controller.Edit(
            new AgencyCategory { AgencyCategoryId = housing.AgencyCategoryId, AgencyCategoryName = "Housing Assistance " });

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual("Housing Assistance", (await _context.AgencyCategory.SingleAsync()).AgencyCategoryName);
    }

    [TestMethod]
    public async Task Edit_ChangesCaseOfOwnName_Saves()
    {
        // Arrange
        AgencyCategory housing = await AddCategoryAsync("housing");

        // Act
        IActionResult result = await _controller.Edit(
            new AgencyCategory { AgencyCategoryId = housing.AgencyCategoryId, AgencyCategoryName = "Housing" });

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual("Housing", (await _context.AgencyCategory.SingleAsync()).AgencyCategoryName);
    }

    [TestMethod]
    public async Task Edit_NameOfAnotherCategory_ReturnsViewWithError()
    {
        // Arrange
        await AddCategoryAsync("Advocacy");
        AgencyCategory housing = await AddCategoryAsync("Housing");

        // Act
        IActionResult result = await _controller.Edit(
            new AgencyCategory { AgencyCategoryId = housing.AgencyCategoryId, AgencyCategoryName = "Advocacy" });

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
        Assert.AreEqual("Housing", (await _context.AgencyCategory.FindAsync(housing.AgencyCategoryId))!.AgencyCategoryName);
    }

    [TestMethod]
    public async Task Edit_InvalidId_ReturnsNotFound()
    {
        // Act
        IActionResult result = await _controller.Edit(new AgencyCategory { AgencyCategoryId = 999, AgencyCategoryName = "Housing" });

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Delete_Get_ShowsAgencyCount()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        _context.Agency.AddRange(
            new Agency { AgencyName = "The Arc of Pierce County", AgencyCategories = { housing } },
            new Agency { AgencyName = "Tacoma Housing Authority", AgencyCategories = { housing } });
        await _context.SaveChangesAsync();

        // Act
        IActionResult result = await _controller.Delete(housing.AgencyCategoryId);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result, out ViewResult view);
        Assert.AreEqual(2, view.ViewData["AgencyCount"]);
    }

    [TestMethod]
    public async Task ConfirmDelete_RemovesCategory_KeepsAgencies()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        _context.Agency.Add(new Agency { AgencyName = "Tacoma Housing Authority", AgencyCategories = { housing } });
        await _context.SaveChangesAsync();

        // Act
        IActionResult result = await _controller.ConfirmDelete(housing.AgencyCategoryId);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(0, await _context.AgencyCategory.CountAsync());
        Assert.AreEqual(1, await _context.Agency.CountAsync());
        Assert.AreEqual("Category \"Housing\" deleted successfully", _controller.TempData["Message"]);
    }
}
