using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class ResourceLinkTests
{
    [TestMethod]
    [DataRow("https://www.thearc.org")]
    [DataRow("http://www.ldawa.org")]
    [DataRow("https://www.co.pierce.wa.us/1986/Aging-Disability-Resources")]
    [DataRow("/PDF/ResourceLinks/Reduced-Cost-Service-Guide-May-2023.pdf")]
    public void IsValidUrl_WebAddressOrSitePath_ReturnsTrue(string url)
    {
        Assert.IsTrue(ResourceLink.IsValidUrl(url));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("www.thearc.org")]
    [DataRow("javascript:alert(1)")]
    [DataRow("ftp://files.example.org")]
    [DataRow("mailto:info@pc2online.org")]
    [DataRow("//evil.example.org")]
    [DataRow("/\\evil.example.org")]
    public void IsValidUrl_InvalidAddress_ReturnsFalse(string? url)
    {
        Assert.IsFalse(ResourceLink.IsValidUrl(url));
    }
}
