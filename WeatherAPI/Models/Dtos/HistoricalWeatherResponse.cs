using WeatherAPI.Models;

namespace WeatherAPI.Models.Dtos;

public sealed record HistoricalWeatherResponse(
    DateOnly Date,
    WeatherMetric Metric,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    IReadOnlyCollection<WeatherReadingResponse> Readings);
