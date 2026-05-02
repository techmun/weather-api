using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherAPI.Authentication;
using WeatherAPI.Data;
using WeatherAPI.Models;
using WeatherAPI.Models.Dtos;
using WeatherAPI.Services;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ITokenService tokenService,
    WeatherDbContext dbContext) : ControllerBase
{
    [HttpPost("token")]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Token([FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        var tokenResponse = await tokenService.CreateTokenAsync(request, cancellationToken);
        if (tokenResponse is null)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials." });
        }

        return Ok(tokenResponse);
    }

    [HttpPost("bootstrap-user")]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BootstrapUserResponse>> BootstrapUser(
        [FromBody] BootstrapUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationProblem("username and password are required.");
        }

        if (await dbContext.ApiUsers.AnyAsync(cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Bootstrap already completed." });
        }

        var user = new ApiUser
        {
            Username = request.Username.Trim(),
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            CreatedUtc = DateTimeOffset.UtcNow,
            IsActive = true
        };

        dbContext.ApiUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new BootstrapUserResponse(user.Id, user.Username, user.CreatedUtc);
        return Created($"/api/auth/users/{user.Id}", response);
    }
}
