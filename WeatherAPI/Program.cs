using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using WeatherAPI.Authentication;
using WeatherAPI.Data;
using WeatherAPI.HostedServices;
using WeatherAPI.Middleware;
using WeatherAPI.Options;
using WeatherAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024;
});

builder.Services.Configure<WeatherApiOptions>(builder.Configuration.GetSection(WeatherApiOptions.SectionName));
builder.Services.Configure<JwtAuthOptions>(builder.Configuration.GetSection(JwtAuthOptions.SectionName));
builder.Services.Configure<UpstreamApiOptions>(builder.Configuration.GetSection(UpstreamApiOptions.SectionName));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(BearerTokenAuthenticationDefaults.SchemeName, new OpenApiSecurityScheme
    {
        Description = "Bearer token required in Authorization header. Example: Bearer <token>",
        Name = HeaderNames.Authorization,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(BearerTokenAuthenticationDefaults.SchemeName, doc, null),
            new List<string>()
        }
    });
});
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy("api", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy
                .SetIsOriginAllowed(_ => false)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("public", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"public:{ipAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("protected", context =>
    {
        var authorizationHeader = context.Request.Headers[HeaderNames.Authorization].ToString();
        var token = "anonymous";
        if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authorizationHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                token = "anonymous";
            }
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            $"protected:{token}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("WeatherDb")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks().AddDbContextCheck<WeatherDbContext>("db");

builder.Services.AddHttpClient<ISingaporeWeatherApiClient, SingaporeWeatherApiClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<WeatherApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    })
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IWeatherIngestionService, WeatherIngestionService>();
builder.Services.AddScoped<IWeatherQueryService, WeatherQueryService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddHostedService<WeatherSyncHostedService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS ApiUsers (
            Id TEXT NOT NULL PRIMARY KEY,
            Username TEXT NOT NULL,
            PasswordHash TEXT NOT NULL,
            CreatedUtc TEXT NOT NULL,
            IsActive INTEGER NOT NULL
        );
        """);
    await dbContext.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_ApiUsers_Username ON ApiUsers(Username);");
    await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS AlertSubscriptions;");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var details = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = details.Status.Value;
        await context.Response.WriteAsJsonAsync(details);
    });
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather API v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors("api");
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<ProtectedEndpointAuditMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program;
