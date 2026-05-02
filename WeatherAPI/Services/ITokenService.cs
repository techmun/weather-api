using WeatherAPI.Models.Dtos;

namespace WeatherAPI.Services;

public interface ITokenService
{
    Task<TokenResponse?> CreateTokenAsync(TokenRequest request, CancellationToken cancellationToken);
}
