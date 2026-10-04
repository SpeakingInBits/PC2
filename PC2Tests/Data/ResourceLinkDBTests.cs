using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class ResourceLinkDBTests
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
    public async Task AddAsync_WithValidLink_AddsToDatabase()
    {
        // Arrange
        var link = new ResourceLink
        {
            Name = "Medicare Resources",
            Url = "https://medicare.com",
            Description = "Medicare insurance plans"
        };

        // Act
        await ResourceLinkDB.AddAsync(_context, link);

        // Assert
        var result = await _context.ResourceLinks.ToListAsync();
        Assert.HasCount(1, result);
        Assert.AreEqual("Medicare Resources", result[0].Name);
        Assert.AreEqual("Medicare insurance plans", result[0].Description);
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsLinksInAlphabeticalOrder()
    {
        // Arrange
        _context.ResourceLinks.AddRange(
            new ResourceLink { Name = "TASH", Url = "https://www.tash.org" },
            new ResourceLink { Name = "ADD Association", Url = "https://www.add.org" },
            new ResourceLink { Name = "Family Voices", Url = "https://familyvoices.org" });
        await _context.SaveChangesAsync();

        // Act
        var result = await ResourceLinkDB.GetAllAsync(_context);

        // Assert
        CollectionAssert.AreEqual(
            new[] { "ADD Association", "Family Voices", "TASH" },
            result.Select(rl => rl.Name).ToArray());
    }

    [TestMethod]
    public async Task GetAllAsync_WithNoLinks_ReturnsEmptyList()
    {
        var result = await ResourceLinkDB.GetAllAsync(_context);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetLinkAsync_WithInvalidId_ReturnsNull()
    {
        var result = await ResourceLinkDB.GetLinkAsync(_context, 999);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateAsync_ChangesLink()
    {
        // Arrange
        var link = new ResourceLink { Name = "Old Name", Url = "https://old.example.org" };
        _context.ResourceLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        link.Name = "New Name";
        link.Url = "https://new.example.org";
        await ResourceLinkDB.UpdateAsync(_context, link);

        // Assert
        var result = await _context.ResourceLinks.FindAsync(link.ResourceLinkId);
        Assert.IsNotNull(result);
        Assert.AreEqual("New Name", result.Name);
        Assert.AreEqual("https://new.example.org", result.Url);
    }

    [TestMethod]
    public async Task DeleteAsync_RemovesOnlyThatLink()
    {
        // Arrange
        var link1 = new ResourceLink { Name = "Link 1", Url = "https://one.example.org" };
        var link2 = new ResourceLink { Name = "Link 2", Url = "https://two.example.org" };
        _context.ResourceLinks.AddRange(link1, link2);
        await _context.SaveChangesAsync();

        // Act
        await ResourceLinkDB.DeleteAsync(_context, link1.ResourceLinkId);

        // Assert
        var result = await _context.ResourceLinks.ToListAsync();
        Assert.HasCount(1, result);
        Assert.AreEqual("Link 2", result[0].Name);
    }

    [TestMethod]
    public async Task DeleteAsync_WithInvalidId_DoesNotThrow()
    {
        await ResourceLinkDB.DeleteAsync(_context, 999);
    }
}
