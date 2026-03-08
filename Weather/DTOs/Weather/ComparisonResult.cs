namespace Weather.DTOs.Weather;

public class ComparisonResult
{
    public double TemperatureDifference { get; set; }
    public string WarmerCity { get; set; } = string.Empty;
    public double WindDifference { get; set; }
    public string LessWindyCity { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}