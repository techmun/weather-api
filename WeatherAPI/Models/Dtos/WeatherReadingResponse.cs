using WeatherAPI.Models;

namespace WeatherAPI.Models.Dtos;

public sealed record WeatherReadingResponse(
    string StationId,
    string StationName,
    WeatherMetric Metric,
    decimal Value,
    string Unit,
    string ReadingType,
    DateTimeOffset TimestampUtc,
    decimal Latitude,
    decimal Longitude);
