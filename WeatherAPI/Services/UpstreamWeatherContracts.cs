using System.Text.Json.Serialization;

namespace WeatherAPI.Services;

public sealed record UpstreamWeatherResult(
    Dictionary<string, UpstreamStation> Stations,
    IReadOnlyCollection<UpstreamReading> Readings,
    string ReadingUnit,
    string ReadingType);

public sealed record UpstreamStation(string Id, string DeviceId, string Name, decimal Latitude, decimal Longitude);

public sealed record UpstreamReading(string StationId, decimal Value, DateTimeOffset TimestampUtc);

public sealed record UpstreamResponseEnvelope(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("errorMsg")] string? ErrorMsg,
    [property: JsonPropertyName("data")] UpstreamResponseData? Data);

public sealed record UpstreamTwentyFourHourForecastEnvelope(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("errorMsg")] string? ErrorMsg,
    [property: JsonPropertyName("data")] UpstreamTwentyFourHourForecastData? Data);

public sealed record UpstreamFourDayForecastEnvelope(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("errorMsg")] string? ErrorMsg,
    [property: JsonPropertyName("data")] UpstreamFourDayForecastData? Data);

public sealed record UpstreamResponseData(
    [property: JsonPropertyName("stations")] IReadOnlyCollection<UpstreamStationItem>? Stations,
    [property: JsonPropertyName("readings")] IReadOnlyCollection<UpstreamReadingBlock>? Readings,
    [property: JsonPropertyName("readingType")] string? ReadingType,
    [property: JsonPropertyName("readingUnit")] string? ReadingUnit);

public sealed record UpstreamStationItem(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("deviceId")] string? DeviceId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("location")] UpstreamStationLocation? Location,
    [property: JsonPropertyName("labelLocation")] UpstreamStationLocation? LabelLocation);

public sealed record UpstreamStationLocation(
    [property: JsonPropertyName("latitude")] decimal Latitude,
    [property: JsonPropertyName("longitude")] decimal Longitude);

public sealed record UpstreamReadingBlock(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("data")] IReadOnlyCollection<UpstreamReadingItem>? Data);

public sealed record UpstreamReadingItem(
    [property: JsonPropertyName("stationId")] string? StationId,
    [property: JsonPropertyName("value")] decimal Value);

public sealed record UpstreamTwentyFourHourForecastData(
    [property: JsonPropertyName("records")] IReadOnlyCollection<UpstreamTwentyFourHourForecastRecord>? Records);

public sealed record UpstreamTwentyFourHourForecastRecord(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("updatedTimestamp")] DateTimeOffset? UpdatedTimestamp,
    [property: JsonPropertyName("general")] UpstreamTwentyFourHourGeneralForecast? General);

public sealed record UpstreamTwentyFourHourGeneralForecast(
    [property: JsonPropertyName("temperature")] UpstreamRangeValue? Temperature,
    [property: JsonPropertyName("relativeHumidity")] UpstreamRangeValue? RelativeHumidity,
    [property: JsonPropertyName("forecast")] UpstreamCodeText? Forecast,
    [property: JsonPropertyName("wind")] UpstreamWindForecast? Wind);

public sealed record UpstreamRangeValue(
    [property: JsonPropertyName("low")] decimal? Low,
    [property: JsonPropertyName("high")] decimal? High,
    [property: JsonPropertyName("unit")] string? Unit);

public sealed record UpstreamCodeText(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("text")] string? Text);

public sealed record UpstreamWindForecast(
    [property: JsonPropertyName("speed")] UpstreamRangeValue? Speed,
    [property: JsonPropertyName("direction")] string? Direction);

public sealed record UpstreamFourDayForecastData(
    [property: JsonPropertyName("records")] IReadOnlyCollection<UpstreamFourDayForecastRecord>? Records);

public sealed record UpstreamFourDayForecastRecord(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("forecasts")] IReadOnlyCollection<UpstreamFourDayForecastItem>? Forecasts);

public sealed record UpstreamFourDayForecastItem(
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("day")] string? Day,
    [property: JsonPropertyName("temperature")] UpstreamRangeValue? Temperature,
    [property: JsonPropertyName("relativeHumidity")] UpstreamRangeValue? RelativeHumidity,
    [property: JsonPropertyName("forecast")] UpstreamForecastSummary? Forecast,
    [property: JsonPropertyName("wind")] UpstreamWindForecast? Wind);

public sealed record UpstreamForecastSummary(
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("text")] string? Text);
