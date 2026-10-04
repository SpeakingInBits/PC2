using System.ComponentModel.DataAnnotations;

namespace PC2.Services;

/// <summary>
/// Google reCAPTCHA settings, bound from the "GoogleReCaptcha" configuration section.
/// </summary>
public class ReCaptchaOptions
{
    public const string SectionName = "GoogleReCaptcha";

    /// <summary>
    /// The reCAPTCHA v3 (score) site key
    /// </summary>
    public string? SiteKey { get; set; }

    /// <summary>
    /// The reCAPTCHA v3 (score) secret key
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// The lowest score (0.0 = likely bot, 1.0 = likely human) that is accepted as a pass.
    /// </summary>
    [Range(0.0, 1.0)]
    public double MinimumScore { get; set; } = 0.5;

    /// <summary>
    /// The reCAPTCHA v2 "I'm not a robot" checkbox site key, shown to visitors who score below <see cref="MinimumScore"/>
    /// </summary>
    public string? CheckboxSiteKey { get; set; }

    /// <summary>
    /// The reCAPTCHA v2 "I'm not a robot" checkbox secret key
    /// </summary>
    public string? CheckboxSecretKey { get; set; }

    public bool IsSiteKeyConfigured => IsConfiguredValue(SiteKey);

    public bool IsSecretKeyConfigured => IsConfiguredValue(SecretKey);

    /// <summary>
    /// True when both checkbox keys are set. Until then, visitors who score too low are turned away as before.
    /// </summary>
    public bool IsCheckboxConfigured => IsConfiguredValue(CheckboxSiteKey) && IsConfiguredValue(CheckboxSecretKey);

    /// <summary>
    /// The committed appsettings.json holds a "Set in secrets" placeholder; real reCAPTCHA keys never
    /// contain whitespace, so any value with whitespace is treated as unconfigured.
    /// </summary>
    private static bool IsConfiguredValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsWhiteSpace);
}
