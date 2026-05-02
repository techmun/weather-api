using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeatherAPI.Authentication;
using WeatherAPI.Data;
using WeatherAPI.Models.Dtos;
using WeatherAPI.Options;

namespace WeatherAPI.Services;

public sealed class JwtTokenService(
    WeatherDbContext dbContext,
    IOptions<JwtAuthOptions> options) : ITokenService
{
    public async Task<TokenResponse?> CreateTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        var config = options.Value;

        var username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await dbContext.ApiUsers
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

        if (user is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(config.SigningKey))
        {
            return null;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(Math.Max(1, config.TokenLifetimeMinutes));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var jwt = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new TokenResponse(accessToken, "Bearer", expiresAtUtc);
    }
}
