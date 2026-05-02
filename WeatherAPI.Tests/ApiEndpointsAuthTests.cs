using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WeatherAPI.HostedServices;
using WeatherAPI.Models;
using WeatherAPI.Models.Dtos;
using WeatherAPI.Services;
using WeatherAPI.Authentication;

namespace WeatherAPI.Tests;

public sealed class ApiEndpointsAuthTests(WeatherApiTestFactory factory) : IClassFixture<WeatherApiTestFactory>
{

    [Theory]
    [InlineData("GET", "/api/stations")]
    [InlineData("GET", "/api/weather/current?stationId=S108")]
    [InlineData("GET", "/api/weather/historical?stationId=S108&metric=AirTemperature&date=2026-05-01")]
    [InlineData("GET", "/api/weather/forecast")]
    public async Task ProtectedEndpoints_WithoutBearerToken_ReturnUnauthorized(string method, string path)
    {
        using var client = factory.CreateClient();

        using var request = CreateRequest(method, path);
        using var response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/stations", HttpStatusCode.OK)]
    [InlineData("GET", "/api/weather/current?stationId=S108", HttpStatusCode.OK)]
    [InlineData("GET", "/api/weather/historical?stationId=S108&metric=AirTemperature&date=2026-05-01", HttpStatusCode.OK)]
    [InlineData("GET", "/api/weather/forecast", HttpStatusCode.OK)]
    public async Task ProtectedEndpoints_WithValidBearerToken_ReturnExpectedStatus(string method, string path, HttpStatusCode expectedStatus)
    {
        using var client = factory.CreateClient();
        var token = await RequestTokenAsync(client, WeatherApiTestFactory.ValidUsername, WeatherApiTestFactory.ValidPassword);

        using var request = CreateRequest(method, path, bearerToken: token);
        using var response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(expectedStatus, response.StatusCode);

    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidBearerToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        using var request = CreateRequest("GET", "/api/stations", includeInvalidBearerToken: true);
        using var response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenEndpoint_WithValidCredentials_ReturnsBearerToken()
    {
        using var client = factory.CreateClient();
        await EnsureBootstrapUserAsync(client, WeatherApiTestFactory.ValidUsername, WeatherApiTestFactory.ValidPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/token")
        {
            Content = JsonContent.Create(new TokenRequest(WeatherApiTestFactory.ValidUsername, WeatherApiTestFactory.ValidPassword))
        };

        using var response = await client.SendAsync(request, CancellationToken.None);
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.Equal("Bearer", payload.TokenType);
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        await EnsureBootstrapUserAsync(client, WeatherApiTestFactory.ValidUsername, WeatherApiTestFactory.ValidPassword);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/token")
        {
            Content = JsonContent.Create(new TokenRequest(WeatherApiTestFactory.ValidUsername, "wrong-password"))
        };

        using var response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BootstrapUserEndpoint_CreatesDbUser_AndTokenEndpointAcceptsCredentials()
    {
        using var bootstrapFactory = new WeatherApiBootstrapTestFactory();
        using var client = bootstrapFactory.CreateClient();

        using var bootstrapRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/bootstrap-user")
        {
            Content = JsonContent.Create(new BootstrapUserRequest("bootstrap-user", "bootstrap-password"))
        };

        using var bootstrapResponse = await client.SendAsync(bootstrapRequest, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, bootstrapResponse.StatusCode);

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/token")
        {
            Content = JsonContent.Create(new TokenRequest("bootstrap-user", "bootstrap-password"))
        };

        using var tokenResponse = await client.SendAsync(tokenRequest, CancellationToken.None);
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        Assert.NotNull(tokenPayload);
        Assert.False(string.IsNullOrWhiteSpace(tokenPayload!.AccessToken));
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ArePublic(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocument_ContainsBearerSecurityScheme()
    {
        using var client = factory.CreateClient();

        var swagger = await client.GetFromJsonAsync<SwaggerDocumentDto>("/swagger/v1/swagger.json", CancellationToken.None);

        Assert.NotNull(swagger);
        Assert.NotNull(swagger!.Components);
        Assert.NotNull(swagger.Components.SecuritySchemes);
        Assert.True(swagger.Components.SecuritySchemes.ContainsKey("Bearer"));
    }

    private static HttpRequestMessage CreateRequest(
        string method,
        string path,
        string? bearerToken = null,
        bool includeInvalidBearerToken = false)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (includeInvalidBearerToken)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "wrong-token");
        }

        return request;
    }

    private static async Task<string> RequestTokenAsync(HttpClient client, string username, string password)
    {
        await EnsureBootstrapUserAsync(client, username, password);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/token")
        {
            Content = JsonContent.Create(new TokenRequest(username, password))
        };

        using var response = await client.SendAsync(request, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: CancellationToken.None);
        Assert.NotNull(tokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse!.AccessToken));

        return tokenResponse.AccessToken;
    }

    private static async Task EnsureBootstrapUserAsync(HttpClient client, string username, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/bootstrap-user")
        {
            Content = JsonContent.Create(new BootstrapUserRequest(username, password))
        };

        using var response = await client.SendAsync(request, CancellationToken.None);
        Assert.True(response.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);
    }

    private sealed class SwaggerDocumentDto
    {
        public SwaggerComponentsDto? Components { get; init; }
    }

    private sealed class SwaggerComponentsDto
    {
        public Dictionary<string, object>? SecuritySchemes { get; init; }
    }
}

public sealed class WeatherApiTestFactory : WebApplicationFactory<Program>
{
    public const string ValidUsername = "api-user";
    public const string ValidPassword = "api-password";
    public const string SigningKey = "dev-signing-key-change-me-32-chars-min";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        var bootstrapDbPath = Path.Combine(Path.GetTempPath(), $"weather-bootstrap-{Guid.NewGuid():N}.db");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WeatherDb"] = $"Data Source={bootstrapDbPath}",
                ["JwtAuth:Issuer"] = "WeatherAPI",
                ["JwtAuth:Audience"] = "WeatherAPI.Clients",
                ["JwtAuth:SigningKey"] = SigningKey,
                ["JwtAuth:TokenLifetimeMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IWeatherQueryService>();
            services.RemoveAll<IWeatherIngestionService>();

            services.AddSingleton<IWeatherQueryService, StubWeatherQueryService>();
            services.AddSingleton<IWeatherIngestionService, StubWeatherIngestionService>();
        });
    }
}

public sealed class WeatherApiBootstrapTestFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "dev-signing-key-change-me-32-chars-min";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var bootstrapDbPath = Path.Combine(Path.GetTempPath(), $"weather-bootstrap-{Guid.NewGuid():N}.db");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:WeatherDb"] = $"Data Source={bootstrapDbPath}",
                ["JwtAuth:Issuer"] = "WeatherAPI",
                ["JwtAuth:Audience"] = "WeatherAPI.Clients",
                ["JwtAuth:SigningKey"] = SigningKey,
                ["JwtAuth:TokenLifetimeMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IWeatherQueryService>();
            services.RemoveAll<IWeatherIngestionService>();

            services.AddSingleton<IWeatherQueryService, StubWeatherQueryService>();
            services.AddSingleton<IWeatherIngestionService, StubWeatherIngestionService>();
        });
    }
}

internal sealed class StubWeatherQueryService : IWeatherQueryService
{
    public Task<IReadOnlyCollection<StationResponse>> GetStationsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<StationResponse> stations =
        [
            new("S108", "Marina Barrage", 1.2799m, 103.8703m)
        ];

        return Task.FromResult(stations);
    }

    public Task<IReadOnlyCollection<WeatherReadingResponse>> GetCurrentAsync(string stationId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<WeatherReadingResponse> readings =
        [
            new(stationId, "Marina Barrage", WeatherMetric.AirTemperature, 28.4m, "C", "current", DateTimeOffset.UtcNow, 1.2799m, 103.8703m)
        ];

        return Task.FromResult(readings);
    }

    public Task<HistoricalWeatherResponse> GetHistoricalAsync(string stationId, WeatherMetric metric, DateOnly date, CancellationToken cancellationToken)
    {
        var fromUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        IReadOnlyCollection<WeatherReadingResponse> readings =
        [
            new(stationId, "Marina Barrage", metric, 27.9m, "C", "historical", fromUtc, 1.2799m, 103.8703m)
        ];

        return Task.FromResult(new HistoricalWeatherResponse(date, metric, fromUtc, toUtc, readings));
    }

    public Task<IReadOnlyCollection<FourDayForecastResponse>> GetForecastAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FourDayForecastResponse> forecast =
        [
            new(DateTimeOffset.UtcNow.AddDays(1), "Monday", 26m, 32m, "Degrees Celsius", 55m, 85m, "Percentage", "Fair and occasionally windy", "Fair", "SSE", 15m, 30m, "km/h")
        ];

        return Task.FromResult(forecast);
    }

}

internal sealed class StubWeatherIngestionService : IWeatherIngestionService
{
    public Task<int> SyncAllAsync(CancellationToken cancellationToken) => Task.FromResult(5);

    public Task<int> SyncDayAsync(CancellationToken cancellationToken, DateOnly day) => Task.FromResult(1);

    public Task<int> SyncMetricDayAsync(WeatherMetric metric, DateOnly day, CancellationToken cancellationToken) => Task.FromResult(1);
}


