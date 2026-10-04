using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PC2.Services;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace PC2.Services.Tests;

[TestClass]
public class ReCaptchaServiceTests
{
    private const string TestSecretKey = "test-secret-key";
    private const string TestCheckboxSiteKey = "test-checkbox-site-key";
    private const string TestCheckboxSecretKey = "test-checkbox-secret-key";

    /// <summary>
    /// Returns a canned response (or throws) instead of calling Google's API.
    /// </summary>
    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;
        private readonly Exception? _exception;

        /// <summary>The form sent to Google by the last request</summary>
        public string? LastRequestBody { get; private set; }

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

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private static ReCaptchaService CreateService(HttpMessageHandler handler, string? secretKey = TestSecretKey, double minimumScore = 0.5,
        bool isCheckboxConfigured = false)
    {
        var options = Options.Create(new ReCaptchaOptions
        {
            SecretKey = secretKey,
            MinimumScore = minimumScore,
            CheckboxSiteKey = isCheckboxConfigured ? TestCheckboxSiteKey : null,
            CheckboxSecretKey = isCheckboxConfigured ? TestCheckboxSecretKey : null
        });

        return new ReCaptchaService(new HttpClient(handler), options, Mock.Of<ILogger<ReCaptchaService>>());
    }

    private static StubHttpMessageHandler GoogleResponse(bool success, double score = 0.9, string action = "submit")
    {
        var body = $$"""{"success": {{success.ToString().ToLowerInvariant()}}, "score": {{score.ToString(System.Globalization.CultureInfo.InvariantCulture)}}, "action": "{{action}}"}""";
        return new StubHttpMessageHandler(HttpStatusCode.OK, body);
    }

    /// <summary>
    /// A reCAPTCHA v2 checkbox response, which has no score or action
    /// </summary>
    private static StubHttpMessageHandler GoogleCheckboxResponse(bool success)
    {
        var body = $$"""{"success": {{success.ToString().ToLowerInvariant()}}, "hostname": "localhost"}""";
        return new StubHttpMessageHandler(HttpStatusCode.OK, body);
    }

    [TestMethod]
    public async Task VerifyAsync_ValidTokenAndMatchingAction_ReturnsPassed()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.9, action: "submit"));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Passed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_EmptyToken_ReturnsFailed()
    {
        var service = CreateService(GoogleResponse(success: true));

        var result = await service.VerifyAsync("", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_MissingSecretKey_ReturnsUnavailable()
    {
        var service = CreateService(GoogleResponse(success: true), secretKey: null);

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Unavailable, result);
    }

    [TestMethod]
    public async Task VerifyAsync_PlaceholderSecretKey_ReturnsUnavailable()
    {
        var service = CreateService(GoogleResponse(success: true), secretKey: "Set in secrets");

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Unavailable, result);
    }

    [TestMethod]
    public async Task VerifyAsync_GoogleReportsFailure_ReturnsFailed()
    {
        var service = CreateService(GoogleResponse(success: false));

        var result = await service.VerifyAsync("invalid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_ActionMismatch_ReturnsFailed()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.9, action: "login"));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_ScoreBelowDefaultThreshold_ReturnsFailed()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.3));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_ScoreBelowConfiguredThreshold_ReturnsFailed()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.6), minimumScore: 0.7);

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_ScoreMeetsConfiguredThreshold_ReturnsPassed()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.7), minimumScore: 0.7);

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Passed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_ScoreBelowThresholdWithCheckboxConfigured_ReturnsChallengeRequired()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.3), isCheckboxConfigured: true);

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.ChallengeRequired, result);
    }

    [TestMethod]
    public async Task VerifyAsync_InvalidTokenWithCheckboxConfigured_ReturnsFailed()
    {
        // Only low scores get the checkbox; real browsers don't send invalid tokens
        var service = CreateService(GoogleResponse(success: false), isCheckboxConfigured: true);

        var result = await service.VerifyAsync("invalid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_ActionMismatchWithCheckboxConfigured_ReturnsFailed()
    {
        var service = CreateService(GoogleResponse(success: true, score: 0.3, action: "login"), isCheckboxConfigured: true);

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
    }

    [TestMethod]
    public async Task VerifyAsync_HttpErrorStatus_ReturnsUnavailable()
    {
        var service = CreateService(new StubHttpMessageHandler(HttpStatusCode.InternalServerError, ""));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Unavailable, result);
    }

    [TestMethod]
    public async Task VerifyAsync_NetworkError_ReturnsUnavailable()
    {
        var service = CreateService(new StubHttpMessageHandler(new HttpRequestException("Network unreachable")));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Unavailable, result);
    }

    [TestMethod]
    public async Task VerifyAsync_Timeout_ReturnsUnavailable()
    {
        // HttpClient surfaces its own timeout as a TaskCanceledException without the caller's token being cancelled
        var service = CreateService(new StubHttpMessageHandler(new TaskCanceledException("The request timed out")));

        var result = await service.VerifyAsync("valid-token", "submit");

        Assert.AreEqual(ReCaptchaVerificationResult.Unavailable, result);
    }

    [TestMethod]
    public async Task VerifyAsync_CallerCancels_Throws()
    {
        var service = CreateService(GoogleResponse(success: true));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => service.VerifyAsync("valid-token", "submit", cts.Token));
    }

    [TestMethod]
    public async Task VerifyCheckboxAsync_GoogleReportsSuccess_ReturnsPassedUsingCheckboxSecretKey()
    {
        var handler = GoogleCheckboxResponse(success: true);
        var service = CreateService(handler, isCheckboxConfigured: true);

        var result = await service.VerifyCheckboxAsync("checkbox-token");

        Assert.AreEqual(ReCaptchaVerificationResult.Passed, result);
        StringAssert.Contains(handler.LastRequestBody, "secret=" + TestCheckboxSecretKey);
        StringAssert.Contains(handler.LastRequestBody, "response=checkbox-token");
    }

    [TestMethod]
    public async Task VerifyCheckboxAsync_GoogleReportsFailure_ReturnsChallengeRequired()
    {
        // e.g. the token expired, so the visitor can check the box again
        var service = CreateService(GoogleCheckboxResponse(success: false), isCheckboxConfigured: true);

        var result = await service.VerifyCheckboxAsync("expired-token");

        Assert.AreEqual(ReCaptchaVerificationResult.ChallengeRequired, result);
    }

    [TestMethod]
    public async Task VerifyCheckboxAsync_EmptyToken_ReturnsChallengeRequired()
    {
        var service = CreateService(GoogleCheckboxResponse(success: true), isCheckboxConfigured: true);

        var result = await service.VerifyCheckboxAsync("");

        Assert.AreEqual(ReCaptchaVerificationResult.ChallengeRequired, result);
    }

    [TestMethod]
    public async Task VerifyCheckboxAsync_CheckboxNotConfigured_ReturnsFailed()
    {
        // A checkbox token can't be genuine when the checkbox is never shown, so it mustn't be accepted as Unavailable
        var handler = GoogleCheckboxResponse(success: true);
        var service = CreateService(handler, isCheckboxConfigured: false);

        var result = await service.VerifyCheckboxAsync("checkbox-token");

        Assert.AreEqual(ReCaptchaVerificationResult.Failed, result);
        Assert.IsNull(handler.LastRequestBody);
    }

    [TestMethod]
    public async Task VerifyCheckboxAsync_NetworkError_ReturnsUnavailable()
    {
        var service = CreateService(new StubHttpMessageHandler(new HttpRequestException("Network unreachable")), isCheckboxConfigured: true);

        var result = await service.VerifyCheckboxAsync("checkbox-token");

        Assert.AreEqual(ReCaptchaVerificationResult.Unavailable, result);
    }

    [TestMethod]
    [DataRow(-0.1)]
    [DataRow(5.0)]
    public void Options_MinimumScoreOutOfRange_FailsValidation(double minimumScore)
    {
        var options = new ReCaptchaOptions { MinimumScore = minimumScore };

        bool isValid = Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("Set in secrets", false)]
    [DataRow("6LcAbCdEfGhIjKlMnOpQrStUvWxYz", true)]
    public void Options_IsSiteKeyConfigured_DetectsPlaceholders(string? siteKey, bool expected)
    {
        var options = new ReCaptchaOptions { SiteKey = siteKey };

        Assert.AreEqual(expected, options.IsSiteKeyConfigured);
    }

    [TestMethod]
    [DataRow(null, null, false)]
    [DataRow("Set in secrets", "Set in secrets", false)]
    [DataRow("6LcAbCdEfGhIjKlMnOpQrStUvWxYz", null, false)]
    [DataRow(null, "6LcAbCdEfGhIjKlMnOpQrStUvWxYz", false)]
    [DataRow("6LcAbCdEfGhIjKlMnOpQrStUvWxYz", "6LcZyXwVuTsRqPoNmLkJiHgFeDcBa", true)]
    public void Options_IsCheckboxConfigured_RequiresBothKeys(string? siteKey, string? secretKey, bool expected)
    {
        var options = new ReCaptchaOptions { CheckboxSiteKey = siteKey, CheckboxSecretKey = secretKey };

        Assert.AreEqual(expected, options.IsCheckboxConfigured);
    }
}
