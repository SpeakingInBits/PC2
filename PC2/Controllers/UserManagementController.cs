using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;
using PC2.Models;

namespace PC2.Controllers;

/// <summary>
/// Controller for admin-only user management: add/remove staff users and promote/demote roles.
/// </summary>
[Authorize(Roles = IdentityHelper.Admin)]
public class UserManagementController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserManagementController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Displays a list of all staff and admin users.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var viewModels = new List<UserManagementViewModel>();

        foreach (var user in users)
        {
            viewModels.Add(new UserManagementViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                IsAdmin = await _userManager.IsInRoleAsync(user, IdentityHelper.Admin),
                IsStaff = await _userManager.IsInRoleAsync(user, IdentityHelper.Staff)
            });
        }

        return View(viewModels);
    }

    /// <summary>
    /// Displays the form to create a new staff user.
    /// </summary>
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Creates a new staff user account.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            return View();
        }

        var user = new IdentityUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, IdentityHelper.Staff);
            TempData["SuccessMessage"] = $"Staff user '{email}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View();
    }

    /// <summary>
    /// Promotes a staff user to the admin role.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Promote(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (!await _userManager.IsInRoleAsync(user, IdentityHelper.Admin))
        {
            await _userManager.AddToRoleAsync(user, IdentityHelper.Admin);
        }
        if (await _userManager.IsInRoleAsync(user, IdentityHelper.Staff))
        {
            await _userManager.RemoveFromRoleAsync(user, IdentityHelper.Staff);
        }

        TempData["SuccessMessage"] = $"User '{user.Email}' has been promoted to Admin.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Demotes an admin user back to the staff role.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Demote(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Prevent demoting yourself
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && currentUser.Id == userId)
        {
            TempData["ErrorMessage"] = "You cannot demote your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, IdentityHelper.Admin))
        {
            await _userManager.RemoveFromRoleAsync(user, IdentityHelper.Admin);
        }
        if (!await _userManager.IsInRoleAsync(user, IdentityHelper.Staff))
        {
            await _userManager.AddToRoleAsync(user, IdentityHelper.Staff);
        }

        TempData["SuccessMessage"] = $"User '{user.Email}' has been demoted to Staff.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Deletes a staff user account. Admins cannot delete their own account.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Prevent deleting yourself
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && currentUser.Id == userId)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["SuccessMessage"] = $"User '{user.Email}' has been deleted.";
        return RedirectToAction(nameof(Index));
    }
}
