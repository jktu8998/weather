namespace Weather.DTOs.Auth;

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}