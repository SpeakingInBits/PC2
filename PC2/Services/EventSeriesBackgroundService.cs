using PC2.Data;
using PC2.Models;

namespace PC2.Services;

/// <summary>
/// Adds upcoming dates to event series that never end, so each one always has dates
/// <see cref="EventRecurrence.OpenEndedHorizonMonths"/> months ahead.
/// </summary>
/// <remarks>
/// Runs when the app starts and then every <see cref="CheckInterval"/>. If the app is asleep for a while,
/// the dates are added the next time it runs.
/// </remarks>
public class EventSeriesBackgroundService : BackgroundService
{
    /// <summary>
    /// How often series are checked
    /// </summary>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EventSeriesBackgroundService> _logger;

    public EventSeriesBackgroundService(IServiceScopeFactory scopeFactory, TimeProvider timeProvider,
        ILogger<EventSeriesBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(CheckInterval, _timeProvider);
        do
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                DateOnly today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
                await EventSeriesDB.ExtendOpenEndedSeries(context, today);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Keep checking; a database outage shouldn't stop future checks
                _logger.LogError(ex, "An error occurred while adding dates to repeating events.");
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
