using System.ComponentModel.DataAnnotations;

namespace Weather.DTOs.Auth;

public class RefreshRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}