using System.ComponentModel.DataAnnotations;

namespace PC2.Services;

/// <summary>
/// Sender.net mailing list settings, bound from the "SenderNet" configuration section.
/// PC2's IDs are only set in the production App Service configuration. While they're blank the site
/// doesn't load Sender.net, so local test sign-ups can't reach PC2's real mailing list.
/// </summary>
public class SenderNetOptions
{
    public const string SectionName = "SenderNet";

    /// <summary>
    /// The IDs are written into the page's JavaScript and HTML, so only letters and numbers are allowed
    /// </summary>
    private const string IdPattern = "^[A-Za-z0-9]*$";

    /// <summary>
    /// The Sender.net account ID from the JavaScript snippet Sender.net provides
    /// </summary>
    [RegularExpression(IdPattern, ErrorMessage = "SenderNet:AccountId may only contain letters and numbers")]
    public string? AccountId { get; set; }

    /// <summary>
    /// The data-sender-form-id of the mailing list sign-up form
    /// </summary>
    [RegularExpression(IdPattern, ErrorMessage = "SenderNet:SignupFormId may only contain letters and numbers")]
    public string? SignupFormId { get; set; }

    /// <summary>
    /// True when both IDs are set. Until then, the sign-up form is left out (with a placeholder in development).
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccountId) && !string.IsNullOrWhiteSpace(SignupFormId);
}
