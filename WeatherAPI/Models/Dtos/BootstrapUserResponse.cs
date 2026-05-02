namespace WeatherAPI.Models.Dtos;

public sealed record BootstrapUserResponse(
    Guid Id,
    string Username,
    DateTimeOffset CreatedUtc);
