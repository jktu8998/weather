namespace Weather.DTOs.Weather;

public class CompareResponse
{
    public WeatherResponse City1 { get; set; } = null!;
    public WeatherResponse City2 { get; set; } = null!;
    public ComparisonResult Comparison { get; set; } = null!;
}