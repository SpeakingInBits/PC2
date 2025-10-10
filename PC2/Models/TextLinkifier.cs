using System.Net;

namespace PC2.Models;

/// <summary>
/// Provides utilities for converting plain text URLs, emails, and phone numbers into clickable links.
/// HTML-encodes input to prevent XSS attacks.
/// </summary>
public static class TextLinkifier
{
    /// <summary>
    /// Converts URLs, email addresses, and phone numbers in plain text to HTML links.
    /// HTML-encodes the input first to prevent XSS attacks.
    /// </summary>
    /// <param name="text">The plain text to process.</param>
    /// <returns>HTML string with clickable links.</returns>
    public static string Linkify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // HTML-encode first to prevent XSS attacks
        string result = WebUtility.HtmlEncode(text);

        // Convert email addresses to mailto links
        result = LinkifyEmails(result);

        // Convert web URLs to hyperlinks
        result = LinkifyUrls(result);

        // Convert phone numbers to tel links
        result = LinkifyPhoneNumbers(result);

        return result;
    }

    /// <summary>
    /// Converts email addresses in text to mailto links.
    /// </summary>
    /// <param name="text">The HTML-encoded text to process.</param>
    /// <returns>Text with email addresses converted to mailto links.</returns>
    public static string LinkifyEmails(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return RegexHelpers.EmailPattern().Replace(text, "<a href=\"mailto:$0\">$0</a>");
    }

    /// <summary>
    /// Converts web URLs (http/https) in text to hyperlinks.
    /// Enhanced to handle trailing punctuation correctly.
    /// </summary>
    /// <param name="text">The HTML-encoded text to process.</param>
    /// <returns>Text with URLs converted to hyperlinks.</returns>
    public static string LinkifyUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return RegexHelpers.UrlPattern().Replace(text, "<a href=\"$0\" target=\"_blank\" rel=\"noopener noreferrer\">$0</a>");
    }

    /// <summary>
    /// Converts phone numbers in text to tel links.
    /// Matches formats like: 123-456-7890, 123.456.7890, or 1234567890
    /// </summary>
    /// <param name="text">The HTML-encoded text to process.</param>
    /// <returns>Text with phone numbers converted to tel links.</returns>
    public static string LinkifyPhoneNumbers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return RegexHelpers.PhonePattern().Replace(text, "<a href=\"tel:$0\">$0</a>");
    }
}
