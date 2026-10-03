using System.ComponentModel.DataAnnotations;

namespace PC2.Services;

/// <summary>
/// Google reCAPTCHA v3 settings, bound from the "GoogleReCaptcha" configuration section.
/// </summary>
public class ReCaptchaOptions
{
    public const string SectionName = "GoogleReCaptcha";

    public string? SiteKey { get; set; }

    public string? SecretKey { get; set; }

    /// <summary>
    /// The lowest score (0.0 = likely bot, 1.0 = likely human) that is accepted as a pass.
    /// </summary>
    [Range(0.0, 1.0)]
    public double MinimumScore { get; set; } = 0.5;

    public bool IsSiteKeyConfigured => IsConfiguredValue(SiteKey);

    public bool IsSecretKeyConfigured => IsConfiguredValue(SecretKey);

    /// <summary>
    /// The committed appsettings.json holds a "Set in secrets" placeholder; real reCAPTCHA keys never
    /// contain whitespace, so any value with whitespace is treated as unconfigured.
    /// </summary>
    private static bool IsConfiguredValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsWhiteSpace);
}
