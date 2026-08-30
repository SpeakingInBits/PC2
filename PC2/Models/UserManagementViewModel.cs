namespace PC2.Models;

/// <summary>
/// View model representing an application user and their roles for the user management page.
/// </summary>
public class UserManagementViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsStaff { get; set; }
}
