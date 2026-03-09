using System.Diagnostics;
using Weather.Interfaces;
using Weather.Models;
using Microsoft.Extensions.Logging;

namespace Weather.Services.WeatherService;

using System.Text.Json;
using Microsoft.Extensions.Options;
using Weather.Configurations;

 
public class OpenWeatherMapProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    public string ProviderName => "OpenWeatherMap";
    private readonly ILogger<OpenWeatherMapProvider> _logger;
    public OpenWeatherMapProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenWeatherMapProvider> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = configuration["WeatherProviders:OpenWeatherMap:Key"] 
                  ?? throw new InvalidOperationException("OpenWeatherMap API key not found");
    }

   public async Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
{
     

    //  Экранирование города для URL
    var escapedCity = Uri.EscapeDataString(city);
    var url = $"data/2.5/weather?q={escapedCity}&appid={_apiKey}&units=metric";

    var stopwatch = Stopwatch.StartNew();

    try
    {
        _logger.LogInformation("Запрос к OpenWeatherMap для города {City}", city);

        // 3. Выполнение HTTP-запроса
        var response = await _httpClient.GetAsync(url, cancellationToken);

        var httpElapsed = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();  

        // 4. Обработка неуспешного статуса
        if (!response.IsSuccessStatusCode)
        {
            string? responseBody = null;
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            _logger.LogWarning("OpenWeatherMap вернул статус {StatusCode} для города {City}. Тело: {ResponseBody}",
                (int)response.StatusCode, city, responseBody ?? "(не удалось прочитать)");
            return null;
        }

        // 5. Чтение и парсинг JSON
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 6. Безопасное извлечение полей
        // Проверяем наличие объекта "main" и поля "temp"
        if (!root.TryGetProperty("main", out var main) ||
            !main.TryGetProperty("temp", out var tempProp) ||
            tempProp.ValueKind != JsonValueKind.Number)
        {
            _logger.LogWarning("Отсутствует или некорректное поле main.temp в ответе OpenWeatherMap для города {City}", city);
            return null;
        }
        var temp = tempProp.GetDouble();

        // Проверяем наличие объекта "wind" и поля "speed"
        if (!root.TryGetProperty("wind", out var wind) ||
            !wind.TryGetProperty("speed", out var speedProp) ||
            speedProp.ValueKind != JsonValueKind.Number)
        {
            _logger.LogWarning("Отсутствует или некорректное поле wind.speed в ответе OpenWeatherMap для города {City}", city);
            return null;
        }
        var windSpeedMps = speedProp.GetDouble();
        var windSpeedKph = windSpeedMps * 3.6;

        // Извлечение описания погоды из массива "weather"
        string condition = "Unknown";
        if (root.TryGetProperty("weather", out var weatherArray) &&
            weatherArray.ValueKind == JsonValueKind.Array &&
            weatherArray.GetArrayLength() > 0)
        {
            var firstWeather = weatherArray[0];
            if (firstWeather.TryGetProperty("description", out var descProp) &&
                descProp.ValueKind == JsonValueKind.String)
            {
                condition = descProp.GetString() ?? "Unknown";
            }
        }

        var parseElapsed = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation("OpenWeatherMap успешно ответил для города {City}. HTTP: {HttpMs} мс, парсинг: {ParseMs} мс, всего: {TotalMs} мс",
            city, httpElapsed, parseElapsed, httpElapsed + parseElapsed);

        return new WeatherData
        {
            City = city,
            TemperatureC = temp,
            WindSpeedKph = windSpeedKph,
            Condition = condition,
            Source = ProviderName
        };
    }
    catch (OperationCanceledException)
    {
        _logger.LogDebug("Запрос к OpenWeatherMap для города {City} отменён", city);
        return null;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Сетевая ошибка при запросе к OpenWeatherMap для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Ошибка парсинга JSON от OpenWeatherMap для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Неожиданная ошибка при запросе к OpenWeatherMap для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
}
}