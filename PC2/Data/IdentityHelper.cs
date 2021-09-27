using Microsoft.AspNetCore.Identity;

namespace PC2.Data
{
    public static class IdentityHelper
    {
        public const string Admin = "Admin";

        internal static async Task CreateRoles(IServiceProvider provider, params string[] roles)
        {
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

            IdentityResult roleResult;

            foreach (string role in roles)
            {
                bool doesRoleExist = await roleManager.RoleExistsAsync(role);
                if (!doesRoleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        internal static void SetIdentityOptions(IdentityOptions options)
        {
            // TODO: Identity - Require confirmed account?
            options.SignIn.RequireConfirmedAccount = false;

            // TODO: Identity - Set your password strength requirements
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 1;
        }

        internal static async Task CreateDefaultAdmin(IServiceProvider serviceProvider, string adminRoleName)
        {
            // TODO: Change credentials for default admin
            const string defaultEmail = "admin@pc2online.org";
            const string defaultUsername = "admin@pc2online.org";
            const string defaultPassword = "Password01#"; // ensure password meets all requirements

            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // If no users are in the database, create the default admin
            if (userManager.Users.Count() == 0)
            {
                var defaultUser = new IdentityUser()
                {
                    Email = defaultEmail,
                    UserName = defaultUsername
                };

                // Create user
                await userManager.CreateAsync(defaultUser, defaultPassword);

                // Add to adminRole
                await userManager.AddToRoleAsync(defaultUser, adminRoleName);
            }
        }
    }
}