using IdentityLogin.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace PC2Tests.Models;

[TestClass]
public class FileEmailSenderTests
{
    private string _directory = null!;
    private FileEmailSender _sender = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "PC2Tests", Guid.NewGuid().ToString());

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileEmailDirectory"] = _directory,
                ["PC2Email"] = "info@example.org",
                ["PC2NoReplyEmail"] = "noreply@example.org"
            })
            .Build();

        _sender = new FileEmailSender(config, Mock.Of<IHostEnvironment>(), Mock.Of<ILogger<FileEmailSender>>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task SendHtmlEmailAsync_SavesEmailToFile()
    {
        var response = await _sender.SendHtmlEmailAsync("feedback@example.org", "Weekly <Feedback>", "Plain text", "<p>Html body</p>");

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        string[] files = Directory.GetFiles(_directory, "*.html");
        Assert.HasCount(1, files);

        string content = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("feedback@example.org", content);
        Assert.Contains("noreply@example.org", content);
        Assert.Contains("Weekly &lt;Feedback&gt;", content);
        Assert.Contains("<p>Html body</p>", content);
        Assert.Contains("Plain text", content);
    }

    [TestMethod]
    public async Task SendEmailAsync_SavesContactEmailToPC2Email()
    {
        await _sender.SendEmailAsync("Test User", "user@example.org", "555-555-5555", "Question", "Hello <there>");

        string content = await File.ReadAllTextAsync(Directory.GetFiles(_directory, "*.html").Single());
        Assert.Contains("info@example.org", content);
        Assert.Contains("Hello &lt;there&gt;", content);
    }

    [TestMethod]
    public void Constructor_NoDirectoryConfigured_UsesDevEmailsInContentRoot()
    {
        var environment = Mock.Of<IHostEnvironment>(e => e.ContentRootPath == _directory);

        var sender = new FileEmailSender(new ConfigurationBuilder().Build(), environment, Mock.Of<ILogger<FileEmailSender>>());

        Assert.AreEqual(Path.Combine(_directory, "DevEmails"), sender.Directory);
    }
}
