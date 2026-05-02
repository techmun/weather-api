using Microsoft.EntityFrameworkCore;
using WeatherAPI.Data;
using WeatherAPI.Models;

namespace WeatherAPI.Services;

public sealed class WeatherIngestionService(
    WeatherDbContext dbContext,
    ISingaporeWeatherApiClient apiClient,
    ILogger<WeatherIngestionService> logger) : IWeatherIngestionService
{
    private static readonly WeatherMetric[] Metrics = Enum.GetValues<WeatherMetric>();

    public async Task<int> SyncAllAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        foreach (var metric in Metrics)
        {
            try
            {
                var result = await apiClient.GetReadingsAsync(metric, null, cancellationToken);
                total += await PersistAsync(metric, result, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Skipping metric {Metric} during SyncAll due to upstream error.", metric);
            }
        }

        return total;
    }

    public async Task<int> SyncDayAsync(CancellationToken cancellationToken, DateOnly day)
    {
        var total = 0;
        foreach (var metric in Metrics)
        {
            total += await SyncMetricDayAsync(metric, day, cancellationToken);
        }

        return total;
    }

    public async Task<int> SyncMetricDayAsync(WeatherMetric metric, DateOnly day, CancellationToken cancellationToken)
    {
        try
        {
            var result = await apiClient.GetReadingsAsync(metric, day, cancellationToken);
            return await PersistAsync(metric, result, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Skipping metric {Metric} during SyncDay {Day} due to upstream error.", metric, day);
            return 0;
        }
    }

    private async Task<int> PersistAsync(WeatherMetric metric, UpstreamWeatherResult result, CancellationToken cancellationToken)
    {
        foreach (var station in result.Stations.Values)
        {
            var existingStation = await dbContext.WeatherStations.FindAsync([station.Id], cancellationToken);
            if (existingStation is null)
            {
                dbContext.WeatherStations.Add(new WeatherStation
                {
                    Id = station.Id,
                    DeviceId = station.DeviceId,
                    Name = station.Name,
                    Latitude = station.Latitude,
                    Longitude = station.Longitude
                });
            }
            else
            {
                existingStation.DeviceId = station.DeviceId;
                existingStation.Name = station.Name;
                existingStation.Latitude = station.Latitude;
                existingStation.Longitude = station.Longitude;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var incomingStationIds = result.Readings.Select(reading => reading.StationId).Distinct().ToList();
        var existing = await dbContext.WeatherReadings
            .Where(reading => reading.Metric == metric && incomingStationIds.Contains(reading.StationId))
            .Select(reading => new { reading.StationId, reading.TimestampUtc })
            .ToListAsync(cancellationToken);

        var existingSet = existing
            .Select(item => (item.StationId, item.TimestampUtc))
            .ToHashSet();

        var newReadings = result.Readings
            .Where(reading => !existingSet.Contains((reading.StationId, reading.TimestampUtc)))
            .Select(reading => new WeatherReading
            {
                StationId = reading.StationId,
                Metric = metric,
                TimestampUtc = reading.TimestampUtc,
                Value = reading.Value,
                Unit = result.ReadingUnit,
                ReadingType = result.ReadingType
            })
            .ToList();

        if (newReadings.Count == 0)
        {
            return 0;
        }

        dbContext.WeatherReadings.AddRange(newReadings);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Persisted {Count} readings for {Metric}", newReadings.Count, metric);
        return newReadings.Count;
    }
}
