namespace Weather.Models;

public class WeatherData
{
    public string City { get; set; } = string.Empty;
    public double TemperatureC { get; set; }
     public double WindSpeedKph { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // имя источника
}