using System.ComponentModel.DataAnnotations;

namespace Weather.DTOs.Weather;

public class CompareRequest
{
    [Required, MinLength(2), MaxLength(2)]
    public List<string> Cities { get; set; } = new();
}