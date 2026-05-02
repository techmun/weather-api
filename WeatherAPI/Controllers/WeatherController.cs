using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherAPI.Authentication;
using WeatherAPI.Models;
using WeatherAPI.Models.Dtos;
using WeatherAPI.Services;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("api/weather")]
[Authorize(AuthenticationSchemes = BearerTokenAuthenticationDefaults.SchemeName)]
public sealed class WeatherController(
    IWeatherQueryService weatherQueryService) : ControllerBase
{
    [HttpGet("current")]
    [EnableRateLimiting("protected")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<WeatherReadingResponse>>> GetCurrentAsync(
        [FromQuery, Required] string? stationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return ValidationProblem("stationId is required.");
        }

        var response = await weatherQueryService.GetCurrentAsync(stationId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("historical")]
    [EnableRateLimiting("protected")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HistoricalWeatherResponse>> GetHistoricalAsync(
        [FromQuery, Required] string? stationId,
        [FromQuery, Required] WeatherMetric? metric,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return ValidationProblem("stationId is required.");
        }

        if (metric is null)
        {
            return ValidationProblem("metric is required.");
        }

        if (date is null)
        {
            return ValidationProblem("date is required.");
        }

        var response = await weatherQueryService.GetHistoricalAsync(stationId, metric.Value, date.Value, cancellationToken);
        return Ok(response);
    }

    [HttpGet("forecast")]
    [EnableRateLimiting("protected")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<FourDayForecastResponse>>> GetForecastAsync(CancellationToken cancellationToken)
    {
        var response = await weatherQueryService.GetForecastAsync(cancellationToken);
        return Ok(response);
    }

}
