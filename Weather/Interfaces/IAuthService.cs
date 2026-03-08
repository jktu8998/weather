using Weather.DTOs.Auth;

namespace Weather.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegistrRequest request);
    Task<TokenResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<AuthResult> DeleteAccountAsync(int userId, string password);
}