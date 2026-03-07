using Weather.Models;

namespace Weather.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> StoreRefreshTokenAsync(int userId, string refreshToken);
    Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken);
}