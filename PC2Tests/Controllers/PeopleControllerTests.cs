using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;
using PC2.Models.ViewModels;
using PC2.Services;
using PC2Tests.Models;
using SkiaSharp;

namespace PC2Tests.Controllers;

/// <summary>
/// Checks a person never ends up pointing at a photo that has been deleted from storage
/// </summary>
[TestClass]
public class PeopleControllerTests
{
    private const string CurrentPhotoName = "person_1_20250101000000.jpg";

    private FakeBlobStorage _storage = null!;
    private FailingSaveInterceptor _saveInterceptor = null!;
    private ApplicationDbContext _context = null!;
    private PeopleController _controller = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _storage = await FakeBlobStorage.StartAsync();

        _saveInterceptor = new FailingSaveInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                      .AddInterceptors(_saveInterceptor)
                      .Options;
        _context = new ApplicationDbContext(options);

        _controller = new PeopleController(_context, _storage.CreateUploader(),
            new ImageService(NullLogger<ImageService>.Instance), NullLogger<PeopleController>.Instance);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.TempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _controller.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
        await _storage.DisposeAsync();
    }

    [TestMethod]
    [DataRow(PersonType.Staff)]
    [DataRow(PersonType.Board)]
    [DataRow(PersonType.SteeringCommittee)]
    public async Task Edit_NewPhoto_SavesItAndDeletesTheCurrentPhoto(PersonType type)
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(type);
        PersonViewModel model = CreateEditModel(person);
        model.PhotoFile = CreateUpload(CreatePng(), "image/png");

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        string? imageUrl = (await GetSavedPersonAsync(person.ID)).ImageUrl;
        Assert.IsNotNull(imageUrl);
        string newPhotoName = imageUrl.Split('/').Last();
        Assert.StartsWith($"person_{person.ID}_", newPhotoName);
        CollectionAssert.AreEquivalent(new[] { newPhotoName }, _storage.BlobNames.ToArray());
    }

    [TestMethod]
    [DataRow(PersonType.Staff)]
    [DataRow(PersonType.Board)]
    [DataRow(PersonType.SteeringCommittee)]
    public async Task Edit_CorruptPhoto_KeepsTheCurrentPhoto(PersonType type)
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(type);
        PersonViewModel model = CreateEditModel(person);
        model.PhotoFile = CreateUpload(Encoding.UTF8.GetBytes("This is not an image"), "image/jpeg");

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        await AssertCurrentPhotoKeptAsync(person.ID);
    }

    [TestMethod]
    public async Task Edit_UploadFails_KeepsTheCurrentPhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);
        PersonViewModel model = CreateEditModel(person);
        model.PhotoFile = CreateUpload(CreatePng(), "image/png");
        _storage.FailUploads = true;

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        await AssertCurrentPhotoKeptAsync(person.ID);
    }

    [TestMethod]
    public async Task Edit_NewPhotoAndSaveFails_KeepsTheCurrentPhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);
        PersonViewModel model = CreateEditModel(person);
        model.PhotoFile = CreateUpload(CreatePng(), "image/png");
        _saveInterceptor.Fail = true;

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(_storage.GetUrl(CurrentPhotoName), (await GetSavedPersonAsync(person.ID)).ImageUrl);
        Assert.Contains(CurrentPhotoName, _storage.BlobNames);
    }

    [TestMethod]
    public async Task Edit_DeletingTheCurrentPhotoFails_StillSavesTheNewPhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);
        PersonViewModel model = CreateEditModel(person);
        model.PhotoFile = CreateUpload(CreatePng(), "image/png");
        _storage.FailDeletes = true;

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        string? imageUrl = (await GetSavedPersonAsync(person.ID)).ImageUrl;
        Assert.IsNotNull(imageUrl);
        Assert.AreNotEqual(_storage.GetUrl(CurrentPhotoName), imageUrl);
        Assert.Contains(imageUrl.Split('/').Last(), _storage.BlobNames);
    }

    [TestMethod]
    public async Task Edit_NoNewPhoto_KeepsTheCurrentPhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);
        PersonViewModel model = CreateEditModel(person);
        model.Name = "Renamed";

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual("Renamed", (await GetSavedPersonAsync(person.ID)).Name);
        await AssertCurrentPhotoKeptAsync(person.ID);
    }

    [TestMethod]
    public async Task Edit_RemovePhoto_ClearsAndDeletesThePhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);
        PersonViewModel model = CreateEditModel(person);
        model.RemovePhoto = true;

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.IsNull((await GetSavedPersonAsync(person.ID)).ImageUrl);
        Assert.IsEmpty(_storage.BlobNames);
    }

    [TestMethod]
    public async Task Edit_RemovePhotoAndSaveFails_KeepsThePhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);
        PersonViewModel model = CreateEditModel(person);
        model.RemovePhoto = true;
        _saveInterceptor.Fail = true;

        // Act
        IActionResult result = await _controller.Edit(model);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        await AssertCurrentPhotoKeptAsync(person.ID);
    }

    [TestMethod]
    public async Task Create_WithPhoto_UploadsThePhoto()
    {
        // Arrange
        var model = new PersonViewModel
        {
            Type = PersonType.Staff,
            Name = "New Staff",
            Email = "new.staff@example.com",
            PhotoFile = CreateUpload(CreatePng(), "image/png")
        };

        // Act
        IActionResult result = await _controller.Create(model);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Staff staff = await _context.StaffMembers.AsNoTracking().SingleAsync();
        Assert.IsNotNull(staff.ImageUrl);
        string photoName = staff.ImageUrl.Split('/').Last();
        Assert.StartsWith("person_0_", photoName);
        CollectionAssert.AreEquivalent(new[] { photoName }, _storage.BlobNames.ToArray());
    }

    [TestMethod]
    public async Task ConfirmDelete_DeletesThePersonAndTheirPhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);

        // Act
        IActionResult result = await _controller.ConfirmDelete(person.ID, PersonType.Staff);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.IsFalse(await _context.People.AnyAsync());
        Assert.IsEmpty(_storage.BlobNames);
    }

    [TestMethod]
    public async Task ConfirmDelete_SaveFails_KeepsThePersonAndTheirPhoto()
    {
        // Arrange
        People person = await AddPersonWithPhotoAsync(PersonType.Staff);
        _saveInterceptor.Fail = true;

        // Act
        IActionResult result = await _controller.ConfirmDelete(person.ID, PersonType.Staff);

        // Assert
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.IsNotNull(_controller.TempData["Message"]);
        await AssertCurrentPhotoKeptAsync(person.ID);
    }

    /// <summary>
    /// Adds a person whose photo is <see cref="CurrentPhotoName"/>
    /// </summary>
    private async Task<People> AddPersonWithPhotoAsync(PersonType type)
    {
        string imageUrl = _storage.AddBlob(CurrentPhotoName, CreatePng());
        People person = type switch
        {
            PersonType.Staff => new Staff { Name = "Jane Doe", Email = "jane.doe@example.com", ImageUrl = imageUrl },
            PersonType.Board => new Board { Name = "Jane Doe", MembershipStart = "2020", ImageUrl = imageUrl },
            _ => new SteeringCommittee { Name = "Jane Doe", ImageUrl = imageUrl }
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return person;
    }

    /// <summary>
    /// Creates the model the edit form posts back for an unchanged person
    /// </summary>
    private static PersonViewModel CreateEditModel(People person) => person switch
    {
        Staff staff => PersonViewModel.FromStaff(staff),
        Board board => PersonViewModel.FromBoard(board),
        SteeringCommittee sc => PersonViewModel.FromSteeringCommittee(sc),
        _ => throw new ArgumentOutOfRangeException(nameof(person))
    };

    private async Task<People> GetSavedPersonAsync(int id)
    {
        return await _context.People.AsNoTracking().SingleAsync(p => p.ID == id);
    }

    private async Task AssertCurrentPhotoKeptAsync(int personId)
    {
        Assert.AreEqual(_storage.GetUrl(CurrentPhotoName), (await GetSavedPersonAsync(personId)).ImageUrl);
        CollectionAssert.AreEquivalent(new[] { CurrentPhotoName }, _storage.BlobNames.ToArray());
    }

    /// <summary>
    /// A photo big enough to be resized
    /// </summary>
    private static byte[] CreatePng()
    {
        using var bitmap = new SKBitmap(500, 400);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static IFormFile CreateUpload(byte[] content, string contentType)
    {
        return new FormFile(new MemoryStream(content), 0, content.Length, nameof(PersonViewModel.PhotoFile), "photo")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    /// <summary>
    /// Makes SaveChanges fail while <see cref="Fail"/> is set, as if the database couldn't be reached
    /// </summary>
    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public bool Fail { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new DbUpdateException("The database couldn't be reached.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
