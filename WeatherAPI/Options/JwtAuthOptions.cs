namespace WeatherAPI.Options;

public sealed class JwtAuthOptions
{
    public const string SectionName = "JwtAuth";

    public string Issuer { get; set; } = "WeatherAPI";

    public string Audience { get; set; } = "WeatherAPI.Clients";

    public string SigningKey { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 60;
}
