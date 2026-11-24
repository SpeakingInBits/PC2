using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace PC2.Data
{
    /// <summary>
    /// Turns off the built-in Identity UI pages the site doesn't use, so their URLs return 404.
    /// Accounts are only created by Admins in User Management, and the site doesn't send Identity
    /// emails, so registration, email confirmation and password reset pages are never needed.
    /// </summary>
    /// <remarks>
    /// Login, Logout, Lockout, AccessDenied, two-factor sign-in and the Manage pages stay available.
    /// /Identity/Account/Register is redirected to the login page in Program.cs.
    /// </remarks>
    public class UnusedIdentityPagesConvention : IPageRouteModelConvention
    {
        public static readonly IReadOnlySet<string> UnusedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/Account/Register",
            "/Account/RegisterConfirmation",
            "/Account/ConfirmEmail",
            "/Account/ConfirmEmailChange",
            "/Account/ResendEmailConfirmation",
            "/Account/ForgotPassword",
            "/Account/ForgotPasswordConfirmation",
            "/Account/ResetPassword",
            "/Account/ResetPasswordConfirmation",
            "/Account/ExternalLogin",
        };

        public void Apply(PageRouteModel model)
        {
            if (string.Equals(model.AreaName, "Identity", StringComparison.OrdinalIgnoreCase)
                && UnusedPages.Contains(model.ViewEnginePath))
            {
                // A page with no routes can't be requested
                model.Selectors.Clear();
            }
        }
    }
}
