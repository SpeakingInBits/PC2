using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class TextLinkifierTests
{
    #region LinkifyEmails Tests

    [TestMethod]
    public void LinkifyEmails_WithValidEmail_CreatesMailtoLink()
    {
        // Arrange
        string input = "Contact me at test@example.com for more info.";
        string expected = "Contact me at <a href=\"mailto:test@example.com\">test@example.com</a> for more info.";

        // Act
        string result = TextLinkifier.LinkifyEmails(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyEmails_WithMultipleEmails_CreatesMultipleLinks()
    {
        // Arrange
        string input = "Email admin@site.com or support@company.org.";
        string expected = "Email <a href=\"mailto:admin@site.com\">admin@site.com</a> or <a href=\"mailto:support@company.org\">support@company.org</a>.";

        // Act
        string result = TextLinkifier.LinkifyEmails(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyEmails_WithEmailContainingPlus_CreatesLink()
    {
        // Arrange
        string input = "user+tag@example.com";
        string expected = "<a href=\"mailto:user+tag@example.com\">user+tag@example.com</a>";

        // Act
        string result = TextLinkifier.LinkifyEmails(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyEmails_WithEmailContainingUnderscore_CreatesLink()
    {
        // Arrange
        string input = "first_last@example.com";
        string expected = "<a href=\"mailto:first_last@example.com\">first_last@example.com</a>";

        // Act
        string result = TextLinkifier.LinkifyEmails(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyEmails_WithNullInput_ReturnsNull()
    {
        // Act
        string? result = TextLinkifier.LinkifyEmails(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LinkifyEmails_WithEmptyString_ReturnsEmpty()
    {
        // Act
        string result = TextLinkifier.LinkifyEmails(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void LinkifyEmails_WithWhitespace_ReturnsWhitespace()
    {
        // Arrange
        string input = "   ";

        // Act
        string result = TextLinkifier.LinkifyEmails(input);

        // Assert
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void LinkifyEmails_WithNoEmail_ReturnsUnchanged()
    {
        // Arrange
        string input = "This text has no email addresses.";

        // Act
        string result = TextLinkifier.LinkifyEmails(input);

        // Assert
        Assert.AreEqual(input, result);
    }

    #endregion

    #region LinkifyUrls Tests

    [TestMethod]
    public void LinkifyUrls_WithHttpUrl_CreatesLink()
    {
        // Arrange
        string input = "Visit http://example.com for details.";
        string expected = "Visit <a href=\"http://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">http://example.com</a> for details.";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithHttpsUrl_CreatesLink()
    {
        // Arrange
        string input = "Check out https://secure.example.com";
        string expected = "Check out <a href=\"https://secure.example.com\" target=\"_blank\" rel=\"noopener noreferrer\">https://secure.example.com</a>";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithUrlContainingPath_CreatesLink()
    {
        // Arrange
        string input = "https://example.com/path/to/page";
        string expected = "<a href=\"https://example.com/path/to/page\" target=\"_blank\" rel=\"noopener noreferrer\">https://example.com/path/to/page</a>";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithUrlContainingQueryString_CreatesLink()
    {
        // Arrange
        string input = "https://example.com/search?q=test&page=1";
        string expected = "<a href=\"https://example.com/search?q=test&page=1\" target=\"_blank\" rel=\"noopener noreferrer\">https://example.com/search?q=test&page=1</a>";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithUrlFollowedByPeriod_ExcludesPeriod()
    {
        // Arrange
        string input = "Visit https://example.com.";
        string expected = "Visit <a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">https://example.com</a>.";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithUrlFollowedByComma_ExcludesComma()
    {
        // Arrange
        string input = "Sites like https://example.com, are useful.";
        string expected = "Sites like <a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">https://example.com</a>, are useful.";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithMultipleUrls_CreatesMultipleLinks()
    {
        // Arrange
        string input = "Visit http://example.com and https://test.org";
        string expected = "Visit <a href=\"http://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">http://example.com</a> and <a href=\"https://test.org\" target=\"_blank\" rel=\"noopener noreferrer\">https://test.org</a>";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithNullInput_ReturnsNull()
    {
        // Act
        string? result = TextLinkifier.LinkifyUrls(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LinkifyUrls_WithEmptyString_ReturnsEmpty()
    {
        // Act
        string result = TextLinkifier.LinkifyUrls(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void LinkifyUrls_WithNoUrl_ReturnsUnchanged()
    {
        // Arrange
        string input = "This text has no URLs.";

        // Act
        string result = TextLinkifier.LinkifyUrls(input);

        // Assert
        Assert.AreEqual(input, result);
    }

    #endregion

    #region LinkifyPhoneNumbers Tests

    [TestMethod]
    public void LinkifyPhoneNumbers_WithDashedFormat_CreatesLink()
    {
        // Arrange
        string input = "Call me at 123-456-7890 anytime.";
        string expected = "Call me at <a href=\"tel:123-456-7890\">123-456-7890</a> anytime.";

        // Act
        string result = TextLinkifier.LinkifyPhoneNumbers(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyPhoneNumbers_WithDottedFormat_CreatesLink()
    {
        // Arrange
        string input = "Phone: 123.456.7890";
        string expected = "Phone: <a href=\"tel:123.456.7890\">123.456.7890</a>";

        // Act
        string result = TextLinkifier.LinkifyPhoneNumbers(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyPhoneNumbers_WithPlainFormat_CreatesLink()
    {
        // Arrange
        string input = "Contact: 1234567890";
        string expected = "Contact: <a href=\"tel:1234567890\">1234567890</a>";

        // Act
        string result = TextLinkifier.LinkifyPhoneNumbers(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyPhoneNumbers_WithMultiplePhones_CreatesMultipleLinks()
    {
        // Arrange
        string input = "Call 123-456-7890 or 987.654.3210";
        string expected = "Call <a href=\"tel:123-456-7890\">123-456-7890</a> or <a href=\"tel:987.654.3210\">987.654.3210</a>";

        // Act
        string result = TextLinkifier.LinkifyPhoneNumbers(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void LinkifyPhoneNumbers_WithNullInput_ReturnsNull()
    {
        // Act
        string? result = TextLinkifier.LinkifyPhoneNumbers(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LinkifyPhoneNumbers_WithEmptyString_ReturnsEmpty()
    {
        // Act
        string result = TextLinkifier.LinkifyPhoneNumbers(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void LinkifyPhoneNumbers_WithNoPhone_ReturnsUnchanged()
    {
        // Arrange
        string input = "This text has no phone numbers.";

        // Act
        string result = TextLinkifier.LinkifyPhoneNumbers(input);

        // Assert
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void LinkifyPhoneNumbers_WithTooFewDigits_DoesNotCreateLink()
    {
        // Arrange
        string input = "Short number: 123-45-6789";

        // Act
        string result = TextLinkifier.LinkifyPhoneNumbers(input);

        // Assert
        Assert.AreEqual(input, result);
    }

    #endregion

    #region Linkify (Main Method) Tests

    [TestMethod]
    public void Linkify_WithMixedContent_CreatesAllLinks()
    {
        // Arrange
        string input = "Contact test@example.com, visit https://example.com, or call 123-456-7890.";
        string expected = "Contact <a href=\"mailto:test@example.com\">test@example.com</a>, visit <a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">https://example.com</a>, or call <a href=\"tel:123-456-7890\">123-456-7890</a>.";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Linkify_WithHtmlCharacters_EncodesAndLinkifies()
    {
        // Arrange
        string input = "<script>alert('xss')</script> Contact: test@example.com";
        string expected = "&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt; Contact: <a href=\"mailto:test@example.com\">test@example.com</a>";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Linkify_WithAmpersand_EncodesAndLinkifies()
    {
        // Arrange
        string input = "A & B Company: https://example.com/search?q=a&b";
        string expected = "A &amp; B Company: <a href=\"https://example.com/search?q=a&amp;b\" target=\"_blank\" rel=\"noopener noreferrer\">https://example.com/search?q=a&amp;b</a>";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Linkify_WithNullInput_ReturnsNull()
    {
        // Act
        string? result = TextLinkifier.Linkify(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Linkify_WithEmptyString_ReturnsEmpty()
    {
        // Act
        string result = TextLinkifier.Linkify(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Linkify_WithWhitespace_ReturnsWhitespace()
    {
        // Arrange
        string input = "   ";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void Linkify_WithPlainText_ReturnsEncodedText()
    {
        // Arrange
        string input = "Just plain text with no links.";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void Linkify_WithQuotes_EncodesQuotes()
    {
        // Arrange
        string input = "He said \"hello\" and visited https://example.com";
        string expected = "He said &quot;hello&quot; and visited <a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">https://example.com</a>";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Linkify_WithLessThanGreaterThan_EncodesSymbols()
    {
        // Arrange
        string input = "Price < $100 & > $50: test@example.com";
        string expected = "Price &lt; $100 &amp; &gt; $50: <a href=\"mailto:test@example.com\">test@example.com</a>";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Linkify_WithAllThreeTypes_CreatesAllLinks()
    {
        // Arrange
        string input = "Email: admin@site.com, Web: https://site.com/page, Phone: 555-123-4567";
        string expected = "Email: <a href=\"mailto:admin@site.com\">admin@site.com</a>, Web: <a href=\"https://site.com/page\" target=\"_blank\" rel=\"noopener noreferrer\">https://site.com/page</a>, Phone: <a href=\"tel:555-123-4567\">555-123-4567</a>";

        // Act
        string result = TextLinkifier.Linkify(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    #endregion
}
