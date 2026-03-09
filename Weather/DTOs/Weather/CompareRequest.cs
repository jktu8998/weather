using System.ComponentModel.DataAnnotations;

namespace Weather.DTOs.Weather;

public class CompareRequest
{
    [Required(ErrorMessage = "Cities list is required")]
    [MinLength(2, ErrorMessage = "You must provide exactly two cities")]
    [MaxLength(2, ErrorMessage = "You must provide exactly two cities")]
    public List<string> Cities { get; set; } = new();
}