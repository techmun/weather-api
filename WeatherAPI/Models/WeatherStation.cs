using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.Models;

public sealed class WeatherStation
{
    [MaxLength(20)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(40)]
    public string DeviceId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public ICollection<WeatherReading> Readings { get; set; } = new List<WeatherReading>();
}
