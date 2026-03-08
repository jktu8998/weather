using Weather.Interfaces;

namespace Weather.Services.WeatherService;

using System.Text.Json;
using Weather.Models;

 
public class WttrInProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WttrInProvider> _logger;
    
    public string ProviderName => "wttr.in";

    public WttrInProvider(HttpClient httpClient, ILogger<WttrInProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            // URL encode для безопасности
            var encodedCity = Uri.EscapeDataString(city);
            var url = $"{encodedCity}?format=j1";
            
            _logger.LogInformation("Запрос к wttr.in для города {City}", city);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("wttr.in вернул статус {StatusCode} для города {City}", 
                    response.StatusCode, city);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Парсим ответ wttr.in
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var currentCondition = root.GetProperty("current_condition")[0];
            
            var temp = double.Parse(currentCondition.GetProperty("temp_C").GetString() ?? "0");
            var windSpeedKph = double.Parse(currentCondition.GetProperty("windspeedKmph").GetString() ?? "0");
            var condition = currentCondition.GetProperty("weatherDesc")[0]
                .GetProperty("value").GetString() ?? "";
            
            _logger.LogInformation("Успешно получены данные от wttr.in для города {City}", city);
            
            return new WeatherData
            {
                City = city,
                TemperatureC = temp,
                WindSpeedKph = windSpeedKph,
                Condition = condition,
                Source = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запросе к wttr.in для города {City}", city);
            return null;
        }
    }
}