namespace Weather.DTOs.Weather;

public class SourceDetail
{
    public string Source { get; set; } = string.Empty;
    public double TemperatureC { get; set; }
    public double WindSpeedKph { get; set; }
    public string Condition { get; set; } = string.Empty;
}