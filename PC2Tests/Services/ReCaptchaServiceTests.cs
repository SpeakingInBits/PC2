using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PC2.Services;
using System.Net;
using System.Text;

namespace PC2.Services.Tests;

[TestClass]
public class ReCaptchaServiceTests
{
    private const string TestSecretKey = "test-secret-key";

    /// <summary>
    /// Returns a canned response (or throws) instead of calling Google's API.
    /// </summary>
    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;
        private readonly Exception? _exception;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public StubHttpMessageHandler(Exception exception)
        {
            _exception = exception;
            _responseBody = string.Empty;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            });
        }
    }

    private static ReCaptchaService CreateService(HttpMessageHandler handler, string? secretKey = TestSecretKey, string? minimumScore = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            ["GoogleReCaptcha:SecretKey"] = secretKey,
            ["GoogleReCaptcha:MinimumScore"] = minimumScore
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        return new ReCaptchaService(httpClientFactory.Object, configuration, Mock.Of<ILogger<ReCaptchaService>>());
    }

    private static StubHttpMessageHandler GoogleResponse(bool success, float score = 0.9f, string action = "submit")
    {
        var body = $$"""{"success": {{success.ToString().ToLowerInvariant()}}, "score": {{score.ToString(System.Globalization.CultureInfo.InvariantCulture)}}, "action": "{{action}}"}""";
        return new StubHttpMessageHandler(HttpStatusCode.OK, body);
    }

    [TestMethod]
    public async Task VerifyAsync_ValidTokenAndMatchingAction_ReturnsTrue()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.9f, action: "submit"));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task VerifyAsync_EmptyToken_ReturnsFalse()
    {
        var service = CreateService(GoogleResponse(success: true));

        var result = await service.VerifyAsync("", "submit");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_MissingSecretKey_ReturnsFalse()
    {
        var service = CreateService(GoogleResponse(success: true), secretKey: null);

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_GoogleReportsFailure_ReturnsFalse()
    {
        var service = CreateService(GoogleResponse(success: false));

        var result = await service.VerifyAsync("invalid-token", "submit");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_ActionMismatch_ReturnsFalse()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.9f, action: "login"));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_ScoreBelowDefaultThreshold_ReturnsFalse()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.3f));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_ScoreBelowConfiguredThreshold_ReturnsFalse()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.6f), minimumScore: "0.7");

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_ScoreMeetsConfiguredThreshold_ReturnsTrue()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.7f), minimumScore: "0.7");

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task VerifyAsync_HttpErrorStatus_ReturnsFalse()
    {
        var service = CreateService(new StubHttpMessageHandler(HttpStatusCode.InternalServerError, ""));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task VerifyAsync_NetworkError_ReturnsFalse()
    {
        var service = CreateService(new StubHttpMessageHandler(new HttpRequestException("Network unreachable")));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.IsFalse(result);
    }
}
