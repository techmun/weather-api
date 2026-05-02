namespace WeatherAPI.Models.Dtos;

public sealed record StationResponse(
    string StationId,
    string StationName,
    decimal Latitude,
    decimal Longitude);
