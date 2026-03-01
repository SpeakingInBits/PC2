using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PC2.Controllers;
using PC2.Data;
using PC2.Models;
using System.Security.Claims;

namespace PC2Tests.Controllers;

[TestClass]
public class UserManagementControllerTests
{
    private Mock<UserManager<IdentityUser>> _userManagerMock = null!;
    private UserManagementController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new UserManagementController(_userManagerMock.Object);

        // Setup mock admin user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-id"),
            new Claim(ClaimTypes.Name, "admin@pc2online.org"),
            new Claim(ClaimTypes.Role, IdentityHelper.Admin)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Setup TempData
        _controller.TempData = new TempDataDictionary(
            _controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _controller.Dispose();
    }

    #region Index Tests

    [TestMethod]
    public async Task Index_WithNoUsers_ReturnsEmptyList()
    {
        // Arrange
        _userManagerMock.Setup(m => m.Users)
            .Returns(new List<IdentityUser>().AsQueryable());

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<UserManagementViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(0, model.Count);
    }

    [TestMethod]
    public async Task Index_WithAdminUser_ReturnsUserWithAdminRole()
    {
        // Arrange
        var adminUser = new IdentityUser { Id = "1", Email = "admin@test.com" };
        _userManagerMock.Setup(m => m.Users)
            .Returns(new List<IdentityUser> { adminUser }.AsQueryable());
        _userManagerMock.Setup(m => m.IsInRoleAsync(adminUser, IdentityHelper.Admin))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.IsInRoleAsync(adminUser, IdentityHelper.Staff))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<UserManagementViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.Count);
        Assert.IsTrue(model[0].IsAdmin);
        Assert.IsFalse(model[0].IsStaff);
    }

    [TestMethod]
    public async Task Index_WithStaffUser_ReturnsUserWithStaffRole()
    {
        // Arrange
        var staffUser = new IdentityUser { Id = "2", Email = "staff@test.com" };
        _userManagerMock.Setup(m => m.Users)
            .Returns(new List<IdentityUser> { staffUser }.AsQueryable());
        _userManagerMock.Setup(m => m.IsInRoleAsync(staffUser, IdentityHelper.Admin))
            .ReturnsAsync(false);
        _userManagerMock.Setup(m => m.IsInRoleAsync(staffUser, IdentityHelper.Staff))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        var model = result.Model as List<UserManagementViewModel>;
        Assert.IsNotNull(model);
        Assert.AreEqual(1, model.Count);
        Assert.IsFalse(model[0].IsAdmin);
        Assert.IsTrue(model[0].IsStaff);
        Assert.AreEqual("staff@test.com", model[0].Email);
    }

    #endregion

    #region Create Tests

    [TestMethod]
    public void Create_Get_ReturnsView()
    {
        // Act
        var result = _controller.Create() as ViewResult;

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task Create_Post_WithValidData_CreatesStaffUserAndRedirects()
    {
        // Arrange
        var newUser = new IdentityUser { Email = "newstaff@test.com" };
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), "Password01#"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), IdentityHelper.Staff))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Create("newstaff@test.com", "Password01#") as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), IdentityHelper.Staff), Times.Once);
    }

    [TestMethod]
    public async Task Create_Post_WithEmptyEmail_ReturnsViewWithError()
    {
        // Act
        var result = await _controller.Create(string.Empty, "Password01#") as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Create_Post_WithEmptyPassword_ReturnsViewWithError()
    {
        // Act
        var result = await _controller.Create("staff@test.com", string.Empty) as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Create_Post_WhenCreateFails_ReturnsViewWithErrors()
    {
        // Arrange
        var errors = new[] { new IdentityError { Description = "Password too weak." } };
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _controller.Create("staff@test.com", "weak") as ViewResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    #endregion

    #region Promote Tests

    [TestMethod]
    public async Task Promote_WithValidStaffUser_PromotesToAdminAndRedirects()
    {
        // Arrange
        var staffUser = new IdentityUser { Id = "2", Email = "staff@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(staffUser);
        _userManagerMock.Setup(m => m.IsInRoleAsync(staffUser, IdentityHelper.Admin)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.IsInRoleAsync(staffUser, IdentityHelper.Staff)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.AddToRoleAsync(staffUser, IdentityHelper.Admin)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.RemoveFromRoleAsync(staffUser, IdentityHelper.Staff)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Promote("2") as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);
        _userManagerMock.Verify(m => m.AddToRoleAsync(staffUser, IdentityHelper.Admin), Times.Once);
        _userManagerMock.Verify(m => m.RemoveFromRoleAsync(staffUser, IdentityHelper.Staff), Times.Once);
    }

    [TestMethod]
    public async Task Promote_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _controller.Promote("nonexistent");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    #endregion

    #region Demote Tests

    [TestMethod]
    public async Task Demote_WithValidAdminUser_DemotesToStaffAndRedirects()
    {
        // Arrange
        var adminUser = new IdentityUser { Id = "3", Email = "otheradmin@test.com" };
        var currentUser = new IdentityUser { Id = "admin-id", Email = "admin@pc2online.org" };
        _userManagerMock.Setup(m => m.FindByIdAsync("3")).ReturnsAsync(adminUser);
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
        _userManagerMock.Setup(m => m.IsInRoleAsync(adminUser, IdentityHelper.Admin)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.IsInRoleAsync(adminUser, IdentityHelper.Staff)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.RemoveFromRoleAsync(adminUser, IdentityHelper.Admin)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(adminUser, IdentityHelper.Staff)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Demote("3") as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);
        _userManagerMock.Verify(m => m.RemoveFromRoleAsync(adminUser, IdentityHelper.Admin), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(adminUser, IdentityHelper.Staff), Times.Once);
    }

    [TestMethod]
    public async Task Demote_WithOwnAccount_ReturnsErrorAndRedirects()
    {
        // Arrange - current user tries to demote themselves
        var currentUser = new IdentityUser { Id = "admin-id", Email = "admin@pc2online.org" };
        _userManagerMock.Setup(m => m.FindByIdAsync("admin-id")).ReturnsAsync(currentUser);
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

        // Act
        var result = await _controller.Demote("admin-id") as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);
        Assert.IsNotNull(_controller.TempData["ErrorMessage"]);
        _userManagerMock.Verify(m => m.RemoveFromRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Demote_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _controller.Demote("nonexistent");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task Delete_WithValidStaffUser_DeletesAndRedirects()
    {
        // Arrange
        var staffUser = new IdentityUser { Id = "4", Email = "staff@test.com" };
        var currentUser = new IdentityUser { Id = "admin-id", Email = "admin@pc2online.org" };
        _userManagerMock.Setup(m => m.FindByIdAsync("4")).ReturnsAsync(staffUser);
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
        _userManagerMock.Setup(m => m.DeleteAsync(staffUser)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Delete("4") as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);
        _userManagerMock.Verify(m => m.DeleteAsync(staffUser), Times.Once);
    }

    [TestMethod]
    public async Task Delete_WithOwnAccount_ReturnsErrorAndRedirects()
    {
        // Arrange - current user tries to delete themselves
        var currentUser = new IdentityUser { Id = "admin-id", Email = "admin@pc2online.org" };
        _userManagerMock.Setup(m => m.FindByIdAsync("admin-id")).ReturnsAsync(currentUser);
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

        // Act
        var result = await _controller.Delete("admin-id") as RedirectToActionResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Index", result.ActionName);
        Assert.IsNotNull(_controller.TempData["ErrorMessage"]);
        _userManagerMock.Verify(m => m.DeleteAsync(It.IsAny<IdentityUser>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _controller.Delete("nonexistent");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    #endregion
}
