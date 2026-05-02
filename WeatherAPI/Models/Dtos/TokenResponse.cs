namespace WeatherAPI.Models.Dtos;

public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);
