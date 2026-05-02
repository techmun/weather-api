using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherAPI.Authentication;
using WeatherAPI.Models.Dtos;
using WeatherAPI.Services;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("api/stations")]
[Authorize(AuthenticationSchemes = BearerTokenAuthenticationDefaults.SchemeName)]
public sealed class StationsController(IWeatherQueryService weatherQueryService) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("protected")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<StationResponse>>> GetStationsAsync(CancellationToken cancellationToken)
    {
        var stations = await weatherQueryService.GetStationsAsync(cancellationToken);
        return Ok(stations);
    }
}
