using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class AgencyCategoryDBTests
{
    private ApplicationDbContext _context = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

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

    [TestMethod]
    public async Task GetCategoriesWithAgencyCountsAsync_ReturnsCategoriesInAlphabeticalOrderWithCounts()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        var advocacy = new AgencyCategory { AgencyCategoryName = "Advocacy" };
        var transportation = new AgencyCategory { AgencyCategoryName = "Transportation" };
        _context.AgencyCategory.AddRange(housing, advocacy, transportation);
        _context.Agency.AddRange(
            new Agency { AgencyName = "The Arc of Pierce County", AgencyCategories = { advocacy, housing } },
            new Agency { AgencyName = "Tacoma Housing Authority", AgencyCategories = { housing } });
        await _context.SaveChangesAsync();

        // Act
        List<AgencyCategoryDisplayViewModel> result = await AgencyCategoryDB.GetCategoriesWithAgencyCountsAsync(_context);

        // Assert
        Assert.HasCount(3, result);
        Assert.AreEqual("Advocacy", result[0].AgencyCategoryName);
        Assert.AreEqual(1, result[0].AgencyCount);
        Assert.AreEqual("Housing", result[1].AgencyCategoryName);
        Assert.AreEqual(2, result[1].AgencyCount);
        Assert.AreEqual("Transportation", result[2].AgencyCategoryName);
        Assert.AreEqual(0, result[2].AgencyCount);
    }

    [TestMethod]
    public async Task GetAgencyCountAsync_ReturnsNumberOfAgenciesInCategory()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        _context.Agency.AddRange(
            new Agency { AgencyName = "The Arc of Pierce County", AgencyCategories = { housing } },
            new Agency { AgencyName = "Tacoma Housing Authority", AgencyCategories = { housing } },
            new Agency { AgencyName = "Pierce Transit" });
        await _context.SaveChangesAsync();

        // Act
        int count = await AgencyCategoryDB.GetAgencyCountAsync(_context, housing.AgencyCategoryId);

        // Assert
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    [DataRow("Housing")]
    [DataRow("housing")]
    [DataRow("  HOUSING  ")]
    public async Task NameExistsAsync_NameUsedByAnotherCategory_ReturnsTrue(string name)
    {
        // Arrange
        _context.AgencyCategory.Add(new AgencyCategory { AgencyCategoryName = "Housing" });
        await _context.SaveChangesAsync();

        // Act
        bool exists = await AgencyCategoryDB.NameExistsAsync(_context, name);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task NameExistsAsync_NewName_ReturnsFalse()
    {
        // Arrange
        _context.AgencyCategory.Add(new AgencyCategory { AgencyCategoryName = "Housing" });
        await _context.SaveChangesAsync();

        // Act
        bool exists = await AgencyCategoryDB.NameExistsAsync(_context, "Housing Assistance");

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task NameExistsAsync_CategoryKeepsItsOwnName_ReturnsFalse()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        _context.AgencyCategory.Add(housing);
        await _context.SaveChangesAsync();

        // Act
        bool exists = await AgencyCategoryDB.NameExistsAsync(_context, "housing", housing.AgencyCategoryId);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task UpdateCategoryAsync_ChangesName_KeepsAgencies()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        _context.Agency.Add(new Agency { AgencyName = "Tacoma Housing Authority", AgencyCategories = { housing } });
        await _context.SaveChangesAsync();

        // Act
        housing.AgencyCategoryName = "Housing Assistance";
        await AgencyCategoryDB.UpdateCategoryAsync(_context, housing);

        // Assert
        using var verifyContext = new ApplicationDbContext(_options);
        AgencyCategory result = await verifyContext.AgencyCategory.Include(a => a.Agencies).SingleAsync();
        Assert.AreEqual("Housing Assistance", result.AgencyCategoryName);
        Assert.HasCount(1, result.Agencies);
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_RemovesCategory_KeepsAgencies()
    {
        // Arrange
        var housing = new AgencyCategory { AgencyCategoryName = "Housing" };
        var advocacy = new AgencyCategory { AgencyCategoryName = "Advocacy" };
        _context.Agency.AddRange(
            new Agency { AgencyName = "The Arc of Pierce County", AgencyCategories = { advocacy, housing } },
            new Agency { AgencyName = "Tacoma Housing Authority", AgencyCategories = { housing } });
        await _context.SaveChangesAsync();
        int housingId = housing.AgencyCategoryId;

        // Act
        await AgencyCategoryDB.DeleteCategoryAsync(_context, housingId);

        // Assert
        using var verifyContext = new ApplicationDbContext(_options);
        Assert.IsNull(await verifyContext.AgencyCategory.FindAsync(housingId));

        List<Agency> agencies = await verifyContext.Agency
            .Include(a => a.AgencyCategories)
            .OrderBy(a => a.AgencyName)
            .ToListAsync();
        Assert.HasCount(2, agencies);
        Assert.AreEqual("Tacoma Housing Authority", agencies[0].AgencyName);
        Assert.IsEmpty(agencies[0].AgencyCategories);
        Assert.AreEqual("The Arc of Pierce County", agencies[1].AgencyName);
        Assert.HasCount(1, agencies[1].AgencyCategories);
        Assert.AreEqual("Advocacy", agencies[1].AgencyCategories[0].AgencyCategoryName);
    }

    [TestMethod]
    public async Task DeleteCategoryAsync_WithInvalidId_DoesNotThrow()
    {
        // Act & Assert - should not throw
        await AgencyCategoryDB.DeleteCategoryAsync(_context, 999);
    }
}
