namespace Weather.Models;

public class GeocodingResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; } = string.Empty;      // Официальное название города
    public string DisplayName { get; set; } = string.Empty; // Полное название с регионом/страной
    public string Country { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
}