using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace PC2.Services;

/// <summary>
/// The outcome of verifying a reCAPTCHA token.
/// </summary>
public enum ReCaptchaVerificationResult
{
    /// <summary>The token is valid, matches the expected action, and meets the minimum score.</summary>
    Passed,

    /// <summary>
    /// The token is missing, invalid, or for a different action, or it scored below the minimum and the
    /// "I'm not a robot" checkbox isn't configured.
    /// </summary>
    Failed,

    /// <summary>
    /// The visitor needs to check the "I'm not a robot" box: their score was below the minimum, or their
    /// checkbox answer couldn't be verified (e.g. it expired). Only returned when the checkbox is configured.
    /// </summary>
    ChallengeRequired,

    /// <summary>
    /// Verification could not be performed (reCAPTCHA is not configured, or Google could not be reached).
    /// Callers decide whether to accept, flag, or reject the submission.
    /// </summary>
    Unavailable
}

public interface IReCaptchaService
{
    /// <summary>
    /// Verifies a Google reCAPTCHA v3 token with Google's API.
    /// </summary>
    /// <param name="token">The reCAPTCHA token from the client-side submission.</param>
    /// <param name="expectedAction">The action name the token was generated for (the value passed to getReCaptchaToken on the client).</param>
    /// <param name="cancellationToken">Cancels the request to Google, e.g. when the client disconnects.</param>
    Task<ReCaptchaVerificationResult> VerifyAsync(string token, string expectedAction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the token from the reCAPTCHA v2 "I'm not a robot" checkbox, shown after a low v3 score.
    /// </summary>
    /// <param name="token">The checkbox token from the client-side submission.</param>
    /// <param name="cancellationToken">Cancels the request to Google, e.g. when the client disconnects.</param>
    Task<ReCaptchaVerificationResult> VerifyCheckboxAsync(string token, CancellationToken cancellationToken = default);
}

public class ReCaptchaService : IReCaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    private readonly HttpClient _httpClient;
    private readonly ReCaptchaOptions _options;
    private readonly ILogger<ReCaptchaService> _logger;

    public ReCaptchaService(HttpClient httpClient, IOptions<ReCaptchaOptions> options, ILogger<ReCaptchaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReCaptchaVerificationResult> VerifyAsync(string token, string expectedAction, CancellationToken cancellationToken = default)
    {
        if (!_options.IsSecretKeyConfigured)
        {
            _logger.LogError("reCAPTCHA verification skipped: GoogleReCaptcha:SecretKey is not configured.");
            return ReCaptchaVerificationResult.Unavailable;
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("reCAPTCHA verification failed: no token was submitted.");
            return ReCaptchaVerificationResult.Failed;
        }

        ReCaptchaResponse? result = await RequestVerificationAsync(_options.SecretKey!, token, cancellationToken);
        if (result is null)
        {
            return ReCaptchaVerificationResult.Unavailable;
        }

        if (!result.Success)
        {
            _logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}", FormatErrorCodes(result));
            return ReCaptchaVerificationResult.Failed;
        }

        if (!string.Equals(result.Action, expectedAction, StringComparison.Ordinal))
        {
            _logger.LogWarning("reCAPTCHA action mismatch. Expected {ExpectedAction} but received {Action}.",
                expectedAction, result.Action);
            return ReCaptchaVerificationResult.Failed;
        }

        if (result.Score < _options.MinimumScore)
        {
            _logger.LogWarning("reCAPTCHA score {Score} is below the minimum threshold of {MinimumScore}.",
                result.Score, _options.MinimumScore);
            return _options.IsCheckboxConfigured
                ? ReCaptchaVerificationResult.ChallengeRequired
                : ReCaptchaVerificationResult.Failed;
        }

        return ReCaptchaVerificationResult.Passed;
    }

    public async Task<ReCaptchaVerificationResult> VerifyCheckboxAsync(string token, CancellationToken cancellationToken = default)
    {
        // The checkbox is never shown without these keys, so a checkbox token is not genuine. Failing (rather than
        // reporting Unavailable, which callers accept) stops it being used to skip the v3 check.
        if (!_options.IsCheckboxConfigured)
        {
            _logger.LogWarning("reCAPTCHA checkbox token rejected: the GoogleReCaptcha checkbox keys are not configured.");
            return ReCaptchaVerificationResult.Failed;
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("reCAPTCHA checkbox verification failed: no token was submitted.");
            return ReCaptchaVerificationResult.ChallengeRequired;
        }

        ReCaptchaResponse? result = await RequestVerificationAsync(_options.CheckboxSecretKey!, token, cancellationToken);
        if (result is null)
        {
            return ReCaptchaVerificationResult.Unavailable;
        }

        if (!result.Success)
        {
            // Usually an expired or reused token, so let the visitor check the box again
            _logger.LogWarning("reCAPTCHA checkbox verification failed. Error codes: {ErrorCodes}", FormatErrorCodes(result));
            return ReCaptchaVerificationResult.ChallengeRequired;
        }

        return ReCaptchaVerificationResult.Passed;
    }

    /// <summary>
    /// Asks Google to verify a token. Returns null when Google couldn't be reached or sent an unusable response.
    /// </summary>
    private async Task<ReCaptchaResponse?> RequestVerificationAsync(string secretKey, string token, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsync(VerifyUrl,
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", secretKey),
                    new KeyValuePair<string, string>("response", token)
                }), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("reCAPTCHA verification request failed with HTTP status {StatusCode}.", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ReCaptchaResponse>(cancellationToken);
            if (result is null)
            {
                _logger.LogWarning("reCAPTCHA verification returned a null response.");
            }
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller (e.g. a disconnected client) gave up; let the cancellation propagate.
            throw;
        }
        catch (Exception ex)
        {
            // Includes HttpClient timeouts, which surface as TaskCanceledException.
            _logger.LogError(ex, "An error occurred while verifying reCAPTCHA token.");
            return null;
        }
    }

    private static string FormatErrorCodes(ReCaptchaResponse result) =>
        result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "none";
}

internal class ReCaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Only sent for v3 tokens
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }

    /// <summary>
    /// Only sent for v3 tokens
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("error-codes")]
    public List<string>? ErrorCodes { get; set; }
}
