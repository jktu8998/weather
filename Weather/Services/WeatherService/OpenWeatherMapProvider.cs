using Weather.Interfaces;
using Weather.Models;

namespace Weather.Services.WeatherService;

using System.Text.Json;
using Microsoft.Extensions.Options;
using Weather.Configurations;

 
public class OpenWeatherMapProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    public string ProviderName => "OpenWeatherMap";

    public OpenWeatherMapProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        // Ключ будет загружаться из конфигурации (User Secrets)
        _apiKey = configuration["WeatherProviders:OpenWeatherMap:Key"] 
                  ?? throw new InvalidOperationException("OpenWeatherMap API key not found");
    }

    public async Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        var url = $"data/2.5/weather?q={city}&appid={_apiKey}&units=metric";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Извлекаем нужные поля
        var temp = root.GetProperty("main").GetProperty("temp").GetDouble();
        var windSpeed = root.GetProperty("wind").GetProperty("speed").GetDouble() * 3.6; // м/с -> км/ч
        var condition = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? "";

        return new WeatherData
        {
            City = city,
            TemperatureC = temp,
            WindSpeedKph = windSpeed,
            Condition = condition,
            Source = ProviderName
        };
    }
}