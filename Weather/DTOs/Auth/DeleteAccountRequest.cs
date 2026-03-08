using System.ComponentModel.DataAnnotations;

namespace Weather.DTOs.Auth;

public class DeleteAccountRequest
{
    [Required(ErrorMessage = "Password is required to confirm deletion")]
    public string Password { get; set; } = string.Empty;
}