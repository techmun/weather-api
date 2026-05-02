using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.Models;

public sealed class WeatherReading
{
    public long Id { get; set; }

    [MaxLength(20)]
    public string StationId { get; set; } = string.Empty;

    public WeatherStation Station { get; set; } = null!;

    public WeatherMetric Metric { get; set; }

    [MaxLength(40)]
    public string ReadingType { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
}
