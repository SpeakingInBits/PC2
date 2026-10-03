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

    /// <summary>The token is missing, invalid, for a different action, or scored below the minimum.</summary>
    Failed,

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

        try
        {
            using var response = await _httpClient.PostAsync(VerifyUrl,
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _options.SecretKey!),
                    new KeyValuePair<string, string>("response", token)
                }), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("reCAPTCHA verification request failed with HTTP status {StatusCode}.", response.StatusCode);
                return ReCaptchaVerificationResult.Unavailable;
            }

            var result = await response.Content.ReadFromJsonAsync<ReCaptchaResponse>(cancellationToken);
            if (result is null)
            {
                _logger.LogWarning("reCAPTCHA verification returned a null response.");
                return ReCaptchaVerificationResult.Unavailable;
            }

            if (!result.Success)
            {
                _logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}",
                    result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "none");
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
                return ReCaptchaVerificationResult.Failed;
            }

            return ReCaptchaVerificationResult.Passed;
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
            return ReCaptchaVerificationResult.Unavailable;
        }
    }
}

internal class ReCaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("error-codes")]
    public List<string>? ErrorCodes { get; set; }
}
