using Microsoft.Extensions.Options;

namespace PC2.Services;

/// <summary>
/// Periodically checks whether the weekly feedback digest email is due and sends it.
/// </summary>
/// <remarks>
/// On Azure App Service, "Always On" should be enabled so the app isn't unloaded when idle. If the app is
/// asleep at the scheduled time, the digest is sent the next time the app runs a check.
/// </remarks>
public class FeedbackDigestBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FeedbackDigestOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FeedbackDigestBackgroundService> _logger;

    public FeedbackDigestBackgroundService(IServiceScopeFactory scopeFactory, IOptions<FeedbackDigestOptions> options,
        TimeProvider timeProvider, ILogger<FeedbackDigestBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Feedback digest emails are disabled (FeedbackDigest:Enabled is false).");
            return;
        }

        using PeriodicTimer timer = new(_options.CheckInterval, _timeProvider);
        do
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                FeedbackDigestService digestService = scope.ServiceProvider.GetRequiredService<FeedbackDigestService>();
                await digestService.SendIfDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Keep checking; a database or email outage shouldn't stop future digests
                _logger.LogError(ex, "An error occurred while checking for the feedback digest.");
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
