namespace WeatherAPI.Services;

using WeatherAPI.Models;

public interface IWeatherIngestionService
{
    Task<int> SyncAllAsync(CancellationToken cancellationToken);

    Task<int> SyncDayAsync(CancellationToken cancellationToken, DateOnly day);

    Task<int> SyncMetricDayAsync(WeatherMetric metric, DateOnly day, CancellationToken cancellationToken);
}
