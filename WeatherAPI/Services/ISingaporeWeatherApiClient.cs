using WeatherAPI.Models;

namespace WeatherAPI.Services;

public interface ISingaporeWeatherApiClient
{
    Task<UpstreamWeatherResult> GetReadingsAsync(WeatherMetric metric, DateOnly? date, CancellationToken cancellationToken);

    Task<UpstreamTwentyFourHourForecastRecord?> GetTwentyFourHourForecastAsync(DateOnly? date, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UpstreamFourDayForecastItem>> GetFourDayForecastAsync(DateOnly? date, CancellationToken cancellationToken);
}
