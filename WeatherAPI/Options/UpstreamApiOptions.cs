namespace WeatherAPI.Options;

public sealed class UpstreamApiOptions
{
    public const string SectionName = "UpstreamApi";

    public string HeaderName { get; set; } = "x-api-key";

    public string Key { get; set; } = string.Empty;
}
