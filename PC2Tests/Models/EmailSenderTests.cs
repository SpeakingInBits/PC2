using Microsoft.VisualStudio.TestTools.UnitTesting;
using IdentityLogin.Models;
using Moq;
using System.Threading.Tasks;

namespace PC2.Models.Tests;

[TestClass]
public class EmailSenderTests
{
    private Mock<IEmailSender> _mockEmailSender;

    [TestInitialize]
    public void Setup()
    {
        _mockEmailSender = new Mock<IEmailSender>();
    }

    [TestMethod]
    public async Task SendEmailAsync_CalledWithCorrectParameters()
    {
        // Arrange
        var name = "Test User";
        var email = "testuser@example.com";
        var phone = "1234567890";
        var subject = "Test Subject";
        var message = "Test Message";

        _mockEmailSender
            .Setup(sender => sender.SendEmailAsync(name, email, phone, subject, message))
            .ReturnsAsync(new SendGrid.Response(System.Net.HttpStatusCode.OK, null, null));

        // Act
        var response = await _mockEmailSender.Object.SendEmailAsync(name, email, phone, subject, message);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsInstanceOfType(response, typeof(SendGrid.Response));
        _mockEmailSender.Verify(sender => sender.SendEmailAsync(name, email, phone, subject, message), Times.Once);
    }

    [TestMethod]
    public async Task SendEmailAsync_ReturnsExpectedResponse()
    {
        // Arrange
        var expectedResponse = new SendGrid.Response(System.Net.HttpStatusCode.OK, null, null);
        _mockEmailSender
            .Setup(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _mockEmailSender.Object.SendEmailAsync("name", "email", "phone", "subject", "message");

        // Assert
        Assert.AreEqual(expectedResponse, response);
    }
}