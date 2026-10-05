using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.IO;

namespace PC2.Configuration.Tests;

[TestClass]
public class AppSettingsConfigTests
{
    private IConfigurationRoot _config;

    [TestInitialize]
    public void Setup()
    {
        // appsettings.json is copied to the test project output directory in the .csproj file
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        _config = builder.Build();
    }

    [TestMethod]
    public void PC2SendGridAPIKey_IsPresent()
    {
        var section = _config["PC2SendGridAPIKey"];
        Assert.IsNotNull(section, "PC2SendGridAPIKey is missing in appsettings.json");
    }

    [TestMethod]
    public void PC2Email_IsPresent()
    {
        var section = _config["PC2Email"];
        Assert.IsNotNull(section, "PC2Email is missing in appsettings.json");
    }

    [TestMethod]
    public void PC2NoReplyEmail_IsPresent()
    {
        var section = _config["PC2NoReplyEmail"];
        Assert.IsNotNull(section, "PC2NoReplyEmail is missing in appsettings.json");
    }

    [TestMethod]
    public void EmailSender_IsSendGrid()
    {
        // "File" only belongs in appsettings.Development.json; in production it would silently stop all email
        Assert.AreEqual("SendGrid", _config["EmailSender"], "EmailSender must be SendGrid in appsettings.json");
    }

    [TestMethod]
    public void DefaultConnection_IsPresentAndNotEmpty()
    {
        var value = _config.GetSection("ConnectionStrings")["DefaultConnection"];
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), "DefaultConnection is missing or empty in appsettings.json");
    }

    [TestMethod]
    public void AzureBlob_ContainerName_IsPresentAndNotEmpty()
    {
        var value = _config.GetSection("AzureBlob")["ContainerName"];
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), "AzureBlob:ContainerName is missing or empty in appsettings.json");
    }

    [TestMethod]
    public void AzureBlob_BlobServiceUri_IsPresentAndNotEmpty()
    {
        var value = _config.GetSection("AzureBlob")["BlobServiceUri"];
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), "AzureBlob:BlobServiceUri is missing or empty in appsettings.json");
    }

    [TestMethod]
    public void GoogleReCaptcha_SiteKey_IsPresent()
    {
        var value = _config["GoogleReCaptcha:SiteKey"];
        Assert.IsNotNull(value, "GoogleReCaptcha:SiteKey is missing in appsettings.json");
    }

    [TestMethod]
    public void GoogleReCaptcha_SecretKey_IsPresent()
    {
        var value = _config["GoogleReCaptcha:SecretKey"];
        Assert.IsNotNull(value, "GoogleReCaptcha:SecretKey is missing in appsettings.json");
    }

    [TestMethod]
    public void GoogleReCaptcha_CheckboxSiteKey_IsPresent()
    {
        var value = _config["GoogleReCaptcha:CheckboxSiteKey"];
        Assert.IsNotNull(value, "GoogleReCaptcha:CheckboxSiteKey is missing in appsettings.json");
    }

    [TestMethod]
    public void GoogleReCaptcha_CheckboxSecretKey_IsPresent()
    {
        var value = _config["GoogleReCaptcha:CheckboxSecretKey"];
        Assert.IsNotNull(value, "GoogleReCaptcha:CheckboxSecretKey is missing in appsettings.json");
    }

    [TestMethod]
    public void GoogleReCaptcha_MinimumScore_IsPresentAndValid()
    {
        var value = _config["GoogleReCaptcha:MinimumScore"];
        Assert.IsNotNull(value, "GoogleReCaptcha:MinimumScore is missing in appsettings.json");
        Assert.IsTrue(float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _), "GoogleReCaptcha:MinimumScore must be a valid number in appsettings.json");
    }

    [TestMethod]
    public void SenderNet_AccountId_IsPresentAndNotEmpty()
    {
        // Only appsettings.Development.json leaves it blank; in production the mailing list form would disappear
        var value = _config["SenderNet:AccountId"];
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), "SenderNet:AccountId is missing or empty in appsettings.json");
    }

    [TestMethod]
    public void SenderNet_SignupFormId_IsPresentAndNotEmpty()
    {
        var value = _config["SenderNet:SignupFormId"];
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), "SenderNet:SignupFormId is missing or empty in appsettings.json");
    }
}
