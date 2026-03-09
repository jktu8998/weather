using Weather.DTOs.Auth;

namespace Weather.Interfaces;

public interface IAuthService
{
    Task  RegisterAsync(RegistrRequest request);
    Task<TokenResponse> LoginAsync(LoginRequest request);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task  ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task  DeleteAccountAsync(int userId, string password);
}