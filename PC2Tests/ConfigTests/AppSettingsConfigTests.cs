using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
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
}
