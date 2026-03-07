namespace Weather.DTOs.Auth;
using System.ComponentModel.DataAnnotations;

public class RegistrRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Password is required")]
    [MinLength(5,ErrorMessage = "Password must be at minimum 6 characters")]
    public string Password { get; set; } = string.Empty;
}