namespace WeatherAPI.Options;

public sealed class WeatherApiOptions
{
    public const string SectionName = "WeatherApi";

    public string BaseUrl { get; set; } = "https://api-open.data.gov.sg/v2/real-time/api/";

    public int PollingIntervalMinutes { get; set; } = 30;

    public int DefaultHistoricalLookbackDays { get; set; } = 2;
}
