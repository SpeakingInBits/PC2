using System.Text.Json.Serialization;

namespace PC2.Services;

public interface IReCaptchaService
{
    /// <summary>
    /// Verifies a Google reCAPTCHA v3 token with Google's API.
    /// </summary>
    /// <param name="token">The reCAPTCHA token from the client-side submission.</param>
    /// <returns>True if the token is valid and the score meets the minimum threshold; otherwise false.</returns>
    Task<bool> VerifyAsync(string token);
}

public class ReCaptchaService : IReCaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";
    private const float DefaultMinimumScore = 0.5f;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReCaptchaService> _logger;
    private readonly string _secretKey;
    private readonly float _minimumScore;

    public ReCaptchaService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ReCaptchaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _secretKey = configuration["GoogleReCaptcha:SecretKey"] ?? string.Empty;
        _minimumScore = float.TryParse(configuration["GoogleReCaptcha:MinimumScore"], out float score)
            ? score
            : DefaultMinimumScore;
    }

    public async Task<bool> VerifyAsync(string token)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(_secretKey))
        {
            _logger.LogWarning("reCAPTCHA verification skipped: token or secret key is missing.");
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(VerifyUrl,
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _secretKey),
                    new KeyValuePair<string, string>("response", token)
                }));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("reCAPTCHA verification request failed with HTTP status {StatusCode}.", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<ReCaptchaResponse>();
            if (result is null)
            {
                _logger.LogWarning("reCAPTCHA verification returned a null response.");
                return false;
            }

            if (!result.Success)
            {
                _logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}",
                    result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "none");
                return false;
            }

            if (result.Score < _minimumScore)
            {
                _logger.LogWarning("reCAPTCHA score {Score} is below the minimum threshold of {MinimumScore}.",
                    result.Score, _minimumScore);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while verifying reCAPTCHA token.");
            return false;
        }
    }
}

internal class ReCaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("error-codes")]
    public List<string>? ErrorCodes { get; set; }
}
