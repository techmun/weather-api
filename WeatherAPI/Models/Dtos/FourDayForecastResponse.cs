namespace WeatherAPI.Models.Dtos;

public sealed record FourDayForecastResponse(
    DateTimeOffset TimestampUtc,
    string Day,
    decimal TemperatureLow,
    decimal TemperatureHigh,
    string TemperatureUnit,
    decimal HumidityLow,
    decimal HumidityHigh,
    string HumidityUnit,
    string ForecastSummary,
    string ForecastText,
    string WindDirection,
    decimal WindSpeedLow,
    decimal WindSpeedHigh,
    string WindSpeedUnit);
