using Microsoft.EntityFrameworkCore;
using PC2.Data;
using PC2.Models;

namespace PC2Tests.Data;

[TestClass]
public class ProgramVideoDBTests
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

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_WithValidVideo_AddsToDatabase()
    {
        // Arrange
        var video = new ProgramVideo
        {
            Title = "Estate Planning",
            YouTubeVideoId = "tRadgduogZg"
        };

        // Act
        await ProgramVideoDB.AddAsync(_context, video);

        // Assert
        var result = await _context.ProgramVideos.ToListAsync();
        Assert.HasCount(1, result);
        Assert.AreEqual("Estate Planning", result[0].Title);
        Assert.AreEqual("tRadgduogZg", result[0].YouTubeVideoId);
    }

    [TestMethod]
    public async Task AddAsync_WithPdfAttachment_AddsToDatabase()
    {
        // Arrange
        var video = new ProgramVideo
        {
            Title = "Guardianships",
            YouTubeVideoId = "qUq_26jU2kU",
            PdfLocation = "https://example.blob.core.windows.net/files/handout.pdf",
            PdfName = "handout.pdf"
        };

        // Act
        await ProgramVideoDB.AddAsync(_context, video);

        // Assert
        var result = await _context.ProgramVideos.FindAsync(video.ProgramVideoId);
        Assert.IsNotNull(result);
        Assert.AreEqual("https://example.blob.core.windows.net/files/handout.pdf", result.PdfLocation);
        Assert.AreEqual("handout.pdf", result.PdfName);
    }

    #endregion

    #region GetAllAsync Tests

    [TestMethod]
    public async Task GetAllAsync_WithNoVideos_ReturnsEmptyList()
    {
        // Act
        var result = await ProgramVideoDB.GetAllAsync(_context);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task GetAllAsync_WithMultipleVideos_ReturnsAll()
    {
        // Arrange
        var video1 = new ProgramVideo { Title = "Video 1", YouTubeVideoId = "abc123" };
        var video2 = new ProgramVideo { Title = "Video 2", YouTubeVideoId = "def456" };
        _context.ProgramVideos.AddRange(video1, video2);
        await _context.SaveChangesAsync();

        // Act
        var result = await ProgramVideoDB.GetAllAsync(_context);

        // Assert
        Assert.HasCount(2, result);
    }

    #endregion

    #region GetVideoAsync Tests

    [TestMethod]
    public async Task GetVideoAsync_WithValidId_ReturnsVideo()
    {
        // Arrange
        var video = new ProgramVideo { Title = "Estate Planning", YouTubeVideoId = "tRadgduogZg" };
        _context.ProgramVideos.Add(video);
        await _context.SaveChangesAsync();

        // Act
        var result = await ProgramVideoDB.GetVideoAsync(_context, video.ProgramVideoId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Estate Planning", result.Title);
    }

    [TestMethod]
    public async Task GetVideoAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await ProgramVideoDB.GetVideoAsync(_context, 999);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region UpdateAsync Tests

    [TestMethod]
    public async Task UpdateAsync_WithValidChanges_UpdatesDatabase()
    {
        // Arrange
        var video = new ProgramVideo { Title = "Old Title", YouTubeVideoId = "abc123" };
        _context.ProgramVideos.Add(video);
        await _context.SaveChangesAsync();

        _context.Entry(video).State = EntityState.Detached;
        video.Title = "New Title";
        video.YouTubeVideoId = "xyz789";

        // Act
        await ProgramVideoDB.UpdateAsync(_context, video);

        // Assert
        var result = await _context.ProgramVideos.FindAsync(video.ProgramVideoId);
        Assert.IsNotNull(result);
        Assert.AreEqual("New Title", result.Title);
        Assert.AreEqual("xyz789", result.YouTubeVideoId);
    }

    #endregion

    #region DeleteAsync Tests

    [TestMethod]
    public async Task DeleteAsync_WithValidId_RemovesFromDatabase()
    {
        // Arrange
        var video = new ProgramVideo { Title = "Estate Planning", YouTubeVideoId = "tRadgduogZg" };
        _context.ProgramVideos.Add(video);
        await _context.SaveChangesAsync();
        int id = video.ProgramVideoId;

        // Act
        await ProgramVideoDB.DeleteAsync(_context, id);

        // Assert
        var result = await _context.ProgramVideos.FindAsync(id);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task DeleteAsync_WithInvalidId_DoesNotThrow()
    {
        // Act & Assert - should not throw
        await ProgramVideoDB.DeleteAsync(_context, 999);
    }

    [TestMethod]
    public async Task DeleteAsync_DeletesOnlySpecifiedVideo()
    {
        // Arrange
        var video1 = new ProgramVideo { Title = "Video 1", YouTubeVideoId = "abc123" };
        var video2 = new ProgramVideo { Title = "Video 2", YouTubeVideoId = "def456" };
        _context.ProgramVideos.AddRange(video1, video2);
        await _context.SaveChangesAsync();

        // Act
        await ProgramVideoDB.DeleteAsync(_context, video1.ProgramVideoId);

        // Assert
        var remaining = await _context.ProgramVideos.ToListAsync();
        Assert.HasCount(1, remaining);
        Assert.AreEqual("Video 2", remaining[0].Title);
    }

    #endregion
}
