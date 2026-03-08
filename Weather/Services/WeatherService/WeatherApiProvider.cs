using System.Diagnostics;
using Weather.Interfaces;
 
namespace Weather.Services.WeatherService;

using System.Text.Json;
using Weather.Models;

 
public class WeatherApiProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<WeatherApiProvider> _logger;
    public string ProviderName => "WeatherAPI";

    public WeatherApiProvider(HttpClient httpClient, IConfiguration configuration,ILogger<WeatherApiProvider> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = configuration["WeatherProviders:WeatherAPI:Key"] 
                  ?? throw new InvalidOperationException("WeatherAPI key not found");
    }

    public async Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var url = $"v1/current.json?key={_apiKey}&q={city}";
            _logger.LogInformation("Запрос к WeatherAPI для города {City}", city);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.GetProperty("current");

            var temp = root.GetProperty("temp_c").GetDouble();
            var windSpeed = root.GetProperty("wind_kph").GetDouble();
            var condition = root.GetProperty("condition").GetProperty("text").GetString() ?? "";

        stopwatch.Stop();
        _logger.LogInformation("WeatherApi ответил за {ElapsedMs} мс для города {City}", stopwatch.ElapsedMilliseconds, city);
            return new WeatherData
            {
                City = city,
                TemperatureC = temp,
                WindSpeedKph = windSpeed,
                Condition = condition,
                Source = ProviderName
            };
        
         
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "Ошибка WeatherApi  через {ElapsedMs} мс для города {City}", stopwatch.ElapsedMilliseconds, city);
        return null;
    }
    }
}