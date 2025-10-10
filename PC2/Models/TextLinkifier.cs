using System.Text.RegularExpressions;

namespace PC2.Models;

/// <summary>
/// Provides utilities for converting plain text URLs, emails, and phone numbers into clickable links.
/// </summary>
public static class TextLinkifier
{
    /// <summary>
    /// Converts URLs, email addresses, and phone numbers in plain text to HTML links.
    /// </summary>
    /// <param name="text">The plain text to process.</param>
    /// <returns>HTML string with clickable links.</returns>
    public static string Linkify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        string result = text;
        result = System.Net.WebUtility.HtmlEncode(result);

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
    /// <param name="text">The text to process.</param>
    /// <returns>Text with email addresses converted to mailto links.</returns>
    public static string LinkifyEmails(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        string emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,63}\b";
        string emailReplacement = "<a href=\"mailto:$0\">$0</a>";
        return Regex.Replace(text, emailPattern, emailReplacement, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Converts web URLs (http/https) in text to hyperlinks.
    /// Enhanced to handle trailing punctuation correctly.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>Text with URLs converted to hyperlinks.</returns>
    public static string LinkifyUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Enhanced pattern to exclude common trailing punctuation
        string webPattern = @"https?://[^\s<>""{}|\\^`\[\]]+[^\s<>""{}|\\^`\[\].,;:!?)]";
        string webReplacement = "<a href=\"$0\" target=\"_blank\" rel=\"noopener noreferrer\">$0</a>";
        return Regex.Replace(text, webPattern, webReplacement);
    }

    /// <summary>
    /// Converts phone numbers in text to tel links.
    /// Matches formats like: 123-456-7890, 123.456.7890, or 1234567890
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>Text with phone numbers converted to tel links.</returns>
    public static string LinkifyPhoneNumbers(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        string phonePattern = @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b";
        string phoneReplacement = "<a href=\"tel:$0\">$0</a>";
        return Regex.Replace(text, phonePattern, phoneReplacement);
    }
}
