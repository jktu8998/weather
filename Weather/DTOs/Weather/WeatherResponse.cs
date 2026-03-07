namespace Weather.DTOs.Weather;

public class WeatherResponse
{
    public string City { get; set; } = string.Empty;
    public WeatherSummary Average { get; set; } = new();
    public List<SourceDetail> Details { get; set; } = new();
}