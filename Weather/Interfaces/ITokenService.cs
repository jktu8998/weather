using Weather.Models;

namespace Weather.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> StoreRefreshTokenAsync(int userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserRefreshTokensAsync(int userId);

}