using WeatherAPI.Models;
using WeatherAPI.Models.Dtos;

namespace WeatherAPI.Services;

public interface IWeatherQueryService
{
    Task<IReadOnlyCollection<StationResponse>> GetStationsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WeatherReadingResponse>> GetCurrentAsync(string stationId, CancellationToken cancellationToken);

    Task<HistoricalWeatherResponse> GetHistoricalAsync(string stationId, WeatherMetric metric, DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FourDayForecastResponse>> GetForecastAsync(CancellationToken cancellationToken);
}
