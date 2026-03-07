using Weather.Models;

namespace Weather.Interfaces;

public interface IWeatherProvider
{
    string ProviderName { get; }
    Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
}