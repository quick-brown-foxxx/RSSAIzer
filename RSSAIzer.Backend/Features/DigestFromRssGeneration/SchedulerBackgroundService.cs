using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features.DigestFromRssGeneration;

/// <summary>
/// Background service that schedules and executes daily digest generation
/// </summary>
[UsedImplicitly]
internal sealed class SchedulerBackgroundService(
    ILogger<SchedulerBackgroundService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    private Timer? _timer;
    private TimeUtc _lastScheduledTimeUtc = new(TimeOnly.MinValue);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Check settings every minute for schedule changes
            using var scope = scopeFactory.CreateScope();
            var settingsManager = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var mainService = scope.ServiceProvider.GetRequiredService<IMainService>();
            await UpdateSchedule(settingsManager, mainService, ct);
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    /// <summary>
    /// Updates the schedule based on settings and creates/updates timer if needed
    /// </summary>
    private async Task UpdateSchedule(
        ISettingsService settingsService,
        IMainService mainService,
        CancellationToken ct
    )
    {
        try
        {
            var settingsResult = await settingsService.LoadSettings(ct);
            if (settingsResult.IsFailed)
            {
                logger.LogError(
                    "Failed to load settings for scheduler: {Errors}",
                    string.Join(", ", settingsResult.Errors)
                );
                return;
            }

            var scheduledTimeUtc = settingsResult.Value.DigestTime;

            // If the schedule hasn't changed, no need to update
            if (scheduledTimeUtc.Time == _lastScheduledTimeUtc.Time)
            {
                return;
            }

            _lastScheduledTimeUtc = scheduledTimeUtc;

            // Calculate time until next run
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);
            var delay = CalculateDelay(now, scheduledTimeUtc.Time);

            logger.LogInformation(
                "Scheduling next digest for {ScheduledTime} (in {Delay})",
                scheduledTimeUtc,
                delay
            );

            // Dispose existing timer if any
            if (_timer != null)
            {
                await _timer.DisposeAsync();
            }

            // Create new timer
            _timer = new(
                _ =>
                    Task.Run(
                        async () =>
                        {
                            try
                            {
                                await ExecuteDigestGeneration(mainService, ct);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Unhandled exception in timer callback");
                            }
                        },
                        ct
                    ),
                null,
                delay,
                TimeSpan.FromDays(1) // Repeat daily
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update digest schedule");
        }
    }

    /// <summary>
    /// Calculates delay until next scheduled time
    /// </summary>
    private static TimeSpan CalculateDelay(TimeOnly now, TimeOnly scheduledTime)
    {
        var delay = scheduledTime - now;
        if (delay < TimeSpan.Zero)
        {
            // If scheduled time has passed today, schedule for tomorrow
            delay = delay.Add(TimeSpan.FromDays(1));
        }
        return delay;
    }

    /// <summary>
    /// Executes digest generation and handles any errors
    /// </summary>
    private async Task ExecuteDigestGeneration(IMainService mainService, CancellationToken ct)
    {
        try
        {
            var digestId = new DigestId();
            logger.LogInformation("Starting scheduled digest generation, id {id}", digestId);

            var now = DateTime.UtcNow;
            var parameters = new DigestParametersModel(
                DateFrom: DateOnly.FromDateTime(now.Date.AddDays(-1)),
                DateTo: DateOnly.FromDateTime(now.Date)
            );

            var queueResult = await mainService.QueueDigest(digestId, parameters, ct);
            if (queueResult.IsFailed)
            {
                logger.LogError(
                    "Failed to queue digest generation: {Errors}",
                    string.Join(", ", queueResult.Errors)
                );
                return;
            }

            // var sendResult = await mainService.SendDigestOverEmail(digestId);
            // if (sendResult.IsFailed)
            // {
            //     logger.LogError(
            //         "Failed to send digest over email: {Errors}",
            //         string.Join(", ", sendResult.Errors)
            //     );
            //     return;
            // }

            logger.LogInformation("Scheduled digest generation queued successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during scheduled digest generation");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Stopping digest scheduler");

        if (_timer != null)
        {
            await _timer.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
