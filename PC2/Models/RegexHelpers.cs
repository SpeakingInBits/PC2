using System.Text.RegularExpressions;

namespace PC2.Models;

/// <summary>
/// Provides source-generated regular expression patterns for identifying URLs, emails, and phone numbers.
/// Using source-generated regex provides optimal performance at compile-time.
/// </summary>
internal static partial class RegexHelpers
{
    /// <summary>
    /// Source-generated regex pattern for matching email addresses.
    /// Matches standard email formats: user@domain.tld
    /// </summary>
    /// <returns>A compiled Regex instance for matching email addresses.</returns>
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,63}\b", 
        RegexOptions.IgnoreCase)]
    public static partial Regex EmailPattern();

    /// <summary>
    /// Source-generated regex pattern for matching HTTP/HTTPS URLs.
    /// Enhanced to exclude common trailing punctuation.
    /// </summary>
    /// <returns>A compiled Regex instance for matching URLs.</returns>
    [GeneratedRegex(@"https?://[^\s<>""{}|\\^`\[\]]+[^\s<>""{}|\\^`\[\].,;:!?)]", 
        RegexOptions.IgnoreCase)]
    public static partial Regex UrlPattern();

    /// <summary>
    /// Source-generated regex pattern for matching phone numbers.
    /// Matches formats: 123-456-7890, 123.456.7890, or 1234567890
    /// </summary>
    /// <returns>A compiled Regex instance for matching phone numbers.</returns>
    [GeneratedRegex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b",
        RegexOptions.None)]
    public static partial Regex PhonePattern();

    /// <summary>
    /// US phone numbers anywhere in an agency's phone text, for <see cref="RendererHelper.RenderPhone"/>.
    /// Matches formats: (xxx) xxx-xxxx, xxx-xxx-xxxx, xxx.xxx.xxxx, xxx xxx xxxx
    /// </summary>
    [GeneratedRegex(@"\(?\d{3}\)?[\s\-.]?\d{3}[\s\-.]?\d{4}")]
    public static partial Regex PhoneNumberInText();

    /// <summary>
    /// A field that is just one phone number, such as (253) 555-1234, optionally followed by one space.
    /// Used by <see cref="Agency.PhoneToString"/>.
    /// </summary>
    [GeneratedRegex(@"^\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}[\s]?$")]
    public static partial Regex WholeFieldPhoneNumber();

    /// <summary>
    /// A field that is exactly one phone number, such as (253) 555-1234. Used by <see cref="Agency.CrisisToString"/>.
    /// </summary>
    [GeneratedRegex(@"^\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}$")]
    public static partial Regex ExactPhoneNumber();
}
