namespace Weather.DTOs.Auth;

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int AccessTokenExpiresIn { get; set; } // время жизни в секундах (опционально)
}