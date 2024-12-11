using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PC2.Data;

namespace PC2.Controllers
{
    [Authorize(Roles = IdentityHelper.Admin)] // Only Admins can access these actions
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // List all users for assigning roles
        public async Task<IActionResult> ManageUsers()
        {
            var users = _userManager.Users.ToList();

            // Create a list of users and their roles
            var model = new List<UserWithRolesViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles
                });
            }

            return View(model); // Pass the model to the view
        }

        // Add AdminLite role to a user
        [HttpPost]
        public async Task<IActionResult> AddAdminLite(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if the user already has the role
            if (await _userManager.IsInRoleAsync(user, IdentityHelper.AdminLite))
            {
                return BadRequest("User is already an AdminLite.");
            }

            // Add the AdminLite role
            await _userManager.AddToRoleAsync(user, IdentityHelper.AdminLite);

            return RedirectToAction("ManageUsers");
        }

        // Remove AdminLite role from a user
        [HttpPost]
        public async Task<IActionResult> RemoveAdminLite(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Remove the AdminLite role
            await _userManager.RemoveFromRoleAsync(user, IdentityHelper.AdminLite);

            return RedirectToAction("ManageUsers");
        }
    }

    public class UserWithRolesViewModel
    {
        public IdentityUser User { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }


}
