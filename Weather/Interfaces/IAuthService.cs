using Weather.DTOs.Auth;

namespace Weather.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegistrRequest request);
    Task<TokenResponse> LoginAsync(LoginRequest request);
}