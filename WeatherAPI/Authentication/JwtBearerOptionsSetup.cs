using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeatherAPI.Options;

namespace WeatherAPI.Authentication;

internal sealed class JwtBearerOptionsSetup(IOptions<JwtAuthOptions> jwtAuthOptions) : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        var config = jwtAuthOptions.Value;

        var signingKey = string.IsNullOrWhiteSpace(config.SigningKey)
            ? new byte[32]   // no valid token can be issued without a configured key; all JWT validation will fail with 401
            : Encoding.UTF8.GetBytes(config.SigningKey);

        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config.Issuer,
            ValidateAudience = true,
            ValidAudience = config.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}
