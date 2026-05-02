using Microsoft.Extensions.Options;
using WeatherAPI.Options;
using WeatherAPI.Services;

namespace WeatherAPI.HostedServices;

public sealed class WeatherSyncHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<WeatherApiOptions> weatherApiOptions,
    ILogger<WeatherSyncHostedService> logger) : BackgroundService
{
    private readonly SemaphoreSlim _runLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Max(1, weatherApiOptions.Value.PollingIntervalMinutes);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!await _runLock.WaitAsync(0, stoppingToken))
            {
                logger.LogWarning("Skipping hosted sync because previous run is still active.");
                continue;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IWeatherIngestionService>();

                using var boundedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                boundedCts.CancelAfter(TimeSpan.FromMinutes(2));

                var inserted = await ingestionService.SyncAllAsync(boundedCts.Token);
                logger.LogInformation("Hosted sync completed. Inserted {InsertedCount} rows.", inserted);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Hosted sync timed out before completion.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hosted sync failed.");
            }
            finally
            {
                _runLock.Release();
            }
        }
    }
}
