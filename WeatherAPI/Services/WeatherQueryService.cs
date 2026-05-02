using Microsoft.EntityFrameworkCore;
using WeatherAPI.Data;
using WeatherAPI.Models;
using WeatherAPI.Models.Dtos;

namespace WeatherAPI.Services;

public sealed class WeatherQueryService(
    WeatherDbContext dbContext,
    ISingaporeWeatherApiClient singaporeWeatherApiClient,
    IWeatherIngestionService ingestionService) : IWeatherQueryService
{
    public async Task<IReadOnlyCollection<StationResponse>> GetStationsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.WeatherStations
            .AsNoTracking()
            .OrderBy(station => station.Name)
            .Select(station => new StationResponse(station.Id, station.Name, station.Latitude, station.Longitude))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WeatherReadingResponse>> GetCurrentAsync(string stationId, CancellationToken cancellationToken)
    {
        await ingestionService.SyncAllAsync(cancellationToken);

        var normalizedStationId = stationId.Trim().ToUpperInvariant();
        var requestedStation = await dbContext.WeatherStations
            .AsNoTracking()
            .FirstOrDefaultAsync(station => station.Id == normalizedStationId, cancellationToken);

        if (requestedStation is null)
        {
            return Array.Empty<WeatherReadingResponse>();
        }

        var allRows = await dbContext.WeatherReadings
            .AsNoTracking()
            .Where(reading => reading.StationId == normalizedStationId)
            .Select(reading => new WeatherReadingResponse(
                reading.StationId,
                reading.Station.Name,
                reading.Metric,
                reading.Value,
                reading.Unit,
                reading.ReadingType,
                reading.TimestampUtc,
                reading.Station.Latitude,
                reading.Station.Longitude))
            .ToListAsync(cancellationToken);

        return allRows
            .GroupBy(reading => reading.Metric)
            .Select(group => group.OrderByDescending(reading => reading.TimestampUtc).First())
            .OrderBy(reading => reading.Metric)
            .ToArray();
    }

    public async Task<HistoricalWeatherResponse> GetHistoricalAsync(string stationId, WeatherMetric metric, DateOnly date, CancellationToken cancellationToken)
    {
        await ingestionService.SyncMetricDayAsync(metric, date, cancellationToken);

        var fromUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var readings = await QueryReadingsAsync(stationId, fromUtc, toUtc, cancellationToken);
        var metricReadings = readings.Where(reading => reading.Metric == metric).ToArray();
        return new HistoricalWeatherResponse(date, metric, fromUtc, toUtc, metricReadings);
    }

    public async Task<IReadOnlyCollection<FourDayForecastResponse>> GetForecastAsync(CancellationToken cancellationToken)
    {
        var forecast = await singaporeWeatherApiClient.GetFourDayForecastAsync(null, cancellationToken);
        return forecast
            .Select(item => new FourDayForecastResponse(
                item.Timestamp,
                item.Day ?? string.Empty,
                item.Temperature?.Low ?? 0,
                item.Temperature?.High ?? item.Temperature?.Low ?? 0,
                item.Temperature?.Unit ?? "Degrees Celsius",
                item.RelativeHumidity?.Low ?? 0,
                item.RelativeHumidity?.High ?? item.RelativeHumidity?.Low ?? 0,
                item.RelativeHumidity?.Unit ?? "Percentage",
                item.Forecast?.Summary ?? string.Empty,
                item.Forecast?.Text ?? string.Empty,
                item.Wind?.Direction ?? string.Empty,
                item.Wind?.Speed?.Low ?? 0,
                item.Wind?.Speed?.High ?? item.Wind?.Speed?.Low ?? 0,
                item.Wind?.Speed?.Unit ?? "km/h"))
            .ToArray();
    }

    private async Task<WeatherReadingResponse[]> QueryReadingsAsync(
        string stationId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var normalizedStationId = stationId.Trim().ToUpperInvariant();

        var stationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalizedStationId };
        var requestedStation = await dbContext.WeatherStations
            .AsNoTracking()
            .FirstOrDefaultAsync(station => station.Id == normalizedStationId, cancellationToken);

        if (requestedStation is not null)
        {
            var relatedStationIds = await dbContext.WeatherStations
                .AsNoTracking()
                .Where(station => station.Name == requestedStation.Name)
                .Select(station => station.Id)
                .ToListAsync(cancellationToken);

            foreach (var id in relatedStationIds)
            {
                stationIds.Add(id);
            }
        }

        var rows = await dbContext.WeatherReadings
            .AsNoTracking()
            .Where(reading => stationIds.Contains(reading.StationId))
            .Select(reading => new WeatherReadingResponse(
                reading.StationId,
                reading.Station.Name,
                reading.Metric,
                reading.Value,
                reading.Unit,
                reading.ReadingType,
                reading.TimestampUtc,
                reading.Station.Latitude,
                reading.Station.Longitude))
            .ToListAsync(cancellationToken);

        IEnumerable<WeatherReadingResponse> filtered = rows;
        if (fromUtc is not null)
        {
            filtered = filtered.Where(reading => reading.TimestampUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            filtered = filtered.Where(reading => reading.TimestampUtc <= toUtc.Value);
        }

        return filtered
            .OrderByDescending(item => item.TimestampUtc)
            .ThenBy(item => item.StationName)
            .ThenBy(item => item.Metric)
            .ToArray();
    }


}
