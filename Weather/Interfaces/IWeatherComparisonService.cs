using Weather.DTOs.Weather;

namespace Weather.Interfaces;

public interface IWeatherComparisonService
{
    Task<CompareResponse?> CompareAsync(string city1, string city2, CancellationToken cancellationToken = default);
}