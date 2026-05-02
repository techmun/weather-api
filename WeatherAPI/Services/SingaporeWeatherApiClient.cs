using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WeatherAPI.Models;
using WeatherAPI.Options;

namespace WeatherAPI.Services;

public sealed class SingaporeWeatherApiClient(
    HttpClient httpClient,
    IOptions<UpstreamApiOptions> upstreamApiOptions) : ISingaporeWeatherApiClient
{
    public async Task<IReadOnlyCollection<UpstreamFourDayForecastItem>> GetFourDayForecastAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var dateQuery = date is null ? string.Empty : $"?date={date.Value:yyyy-MM-dd}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"four-day-outlook{dateQuery}");

        var key = upstreamApiOptions.Value.Key;
        if (!string.IsNullOrWhiteSpace(key))
        {
            request.Headers.TryAddWithoutValidation(upstreamApiOptions.Value.HeaderName, key);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Array.Empty<UpstreamFourDayForecastItem>();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Upstream request failed for 4-day forecast with status {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<UpstreamFourDayForecastEnvelope>(cancellationToken)
            ?? throw new InvalidOperationException("Upstream 4-day forecast payload was empty.");

        return payload.Data?.Records?
            .OrderByDescending(record => record.Timestamp)
            .FirstOrDefault()?.Forecasts?
            .OrderBy(item => item.Timestamp)
            .ToArray()
            ?? Array.Empty<UpstreamFourDayForecastItem>();
    }

    public async Task<UpstreamTwentyFourHourForecastRecord?> GetTwentyFourHourForecastAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var dateQuery = date is null ? string.Empty : $"?date={date.Value:yyyy-MM-dd}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"twenty-four-hr-forecast{dateQuery}");

        var key = upstreamApiOptions.Value.Key;
        if (!string.IsNullOrWhiteSpace(key))
        {
            request.Headers.TryAddWithoutValidation(upstreamApiOptions.Value.HeaderName, key);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Upstream request failed for 24-hour forecast with status {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<UpstreamTwentyFourHourForecastEnvelope>(cancellationToken)
            ?? throw new InvalidOperationException("Upstream 24-hour forecast payload was empty.");

        return payload.Data?.Records?
            .OrderByDescending(record => record.Timestamp)
            .FirstOrDefault();
    }

    public async Task<UpstreamWeatherResult> GetReadingsAsync(WeatherMetric metric, DateOnly? date, CancellationToken cancellationToken)
    {
        var relativePath = metric switch
        {
            WeatherMetric.AirTemperature => "air-temperature",
            WeatherMetric.Rainfall => "rainfall",
            WeatherMetric.RelativeHumidity => "relative-humidity",
            WeatherMetric.WindDirection => "wind-direction",
            WeatherMetric.WindSpeed => "wind-speed",
            _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unsupported metric")
        };

        var dateQuery = date is null ? string.Empty : $"?date={date.Value:yyyy-MM-dd}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{relativePath}{dateQuery}");

        var key = upstreamApiOptions.Value.Key;
        if (!string.IsNullOrWhiteSpace(key))
        {
            request.Headers.TryAddWithoutValidation(upstreamApiOptions.Value.HeaderName, key);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new UpstreamWeatherResult(
                new Dictionary<string, UpstreamStation>(),
                Array.Empty<UpstreamReading>(),
                GetFallbackUnit(metric),
                metric.ToString());
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Upstream request failed for metric '{metric}' with status {(int)response.StatusCode} ({response.StatusCode}).",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<UpstreamResponseEnvelope>(cancellationToken)
            ?? throw new InvalidOperationException("Upstream payload was empty.");
        if (payload.Data is null)
        {
            throw new InvalidOperationException($"Upstream returned no data for metric '{metric}'.");
        }

        var stations = payload.Data.Stations?
            .Where(station => !string.IsNullOrWhiteSpace(station.Id) && (station.Location is not null || station.LabelLocation is not null))
            .ToDictionary(
                station => station.Id!,
                station =>
                {
                    var location = station.Location ?? station.LabelLocation!;
                    return new UpstreamStation(
                        station.Id!,
                        station.DeviceId ?? station.Id!,
                        station.Name ?? station.Id!,
                        location.Latitude,
                        location.Longitude);
                })
            ?? new Dictionary<string, UpstreamStation>();

        var readings = new List<UpstreamReading>();
        foreach (var block in payload.Data.Readings ?? [])
        {
            foreach (var value in block.Data ?? [])
            {
                if (string.IsNullOrWhiteSpace(value.StationId))
                {
                    continue;
                }

                readings.Add(new UpstreamReading(value.StationId, value.Value, block.Timestamp.ToUniversalTime()));
            }
        }

        var unit = payload.Data.ReadingUnit ?? GetFallbackUnit(metric);
        var readingType = string.IsNullOrWhiteSpace(payload.Data.ReadingType) ? metric.ToString() : payload.Data.ReadingType;
        return new UpstreamWeatherResult(stations, readings, unit, readingType);
    }

    private static string GetFallbackUnit(WeatherMetric metric)
    {
        return metric switch
        {
            WeatherMetric.AirTemperature => "C",
            WeatherMetric.Rainfall => "mm",
            WeatherMetric.RelativeHumidity => "%",
            WeatherMetric.WindDirection => "deg",
            WeatherMetric.WindSpeed => "km/h",
            _ => string.Empty
        };
    }
}
