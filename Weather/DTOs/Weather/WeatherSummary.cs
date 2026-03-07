namespace Weather.DTOs.Weather;

public class WeatherSummary
{
    public double TemperatureC { get; set; }
    public double WindSpeedKph { get; set; }
    public string Condition { get; set; } = string.Empty;
}