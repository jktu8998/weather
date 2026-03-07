using Weather.DTOs.Weather;

namespace Weather.Interfaces;

public interface IWeatherAggregator
{
    Task<WeatherResponse?> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
    Task<WeatherResponse?> GetWeatherWithCacheAsync(string city, CancellationToken cancellationToken = default);
}