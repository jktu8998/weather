namespace Weather.Services.WeatherService;

using System.Diagnostics;
using Weather.Interfaces;
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
    // 1. Валидация входных данных
    if (string.IsNullOrWhiteSpace(city))
    {
        _logger.LogWarning("GetWeatherAsync вызван с пустым городом");
        return null;
    }

    // Экранирование города для URL
    var escapedCity = Uri.EscapeDataString(city);
    var url = $"v1/current.json?key={_apiKey}&q={escapedCity}";

    var stopwatch = Stopwatch.StartNew();

    try
    {
        _logger.LogInformation("Запрос к WeatherAPI для города {City} по URL {Url}", city, url);

        // Выполняем HTTP-запрос с поддержкой отмены
        var response = await _httpClient.GetAsync(url, cancellationToken);

        // Замеряем время получения ответа
        var elapsedHttp = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart(); // сбрасываем для замера парсинга

        // Обработка неуспешного статуса
        if (!response.IsSuccessStatusCode)
        {
            // Пытаемся прочитать тело ответа для логирования (только при предупреждении)
            string? responseBody = null;
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }

            _logger.LogWarning("WeatherAPI вернул статус {StatusCode} для города {City}. Тело ответа: {ResponseBody}",
                (int)response.StatusCode, city, responseBody ?? "(не удалось прочитать)");
            return null;
        }

        // Читаем и парсим JSON
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedStopwatch = Stopwatch.StartNew();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.GetProperty("current");

        var temp = root.GetProperty("temp_c").GetDouble();
        var windSpeed = root.GetProperty("wind_kph").GetDouble();
        var condition = root.GetProperty("condition").GetProperty("text").GetString() ?? "";

        var elapsedParse = parsedStopwatch.ElapsedMilliseconds;

        // Логируем успех с детализацией по времени
        _logger.LogInformation("WeatherAPI успешно ответил для города {City}. " +
                               "HTTP: {HttpMs} мс, парсинг: {ParseMs} мс, всего: {TotalMs} мс",
                               city, elapsedHttp, elapsedParse, stopwatch.ElapsedMilliseconds);

        return new WeatherData
        {
            City = city,
            TemperatureC = temp,
            WindSpeedKph = windSpeed,
            Condition = condition,
            Source = ProviderName
        };
    }
    catch (OperationCanceledException)
    {
        // Отмена запроса – не логируем как ошибку, только на Debug
        _logger.LogDebug("Запрос к WeatherAPI для города {City} был отменён", city);
        return null;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Сетевая ошибка при запросе к WeatherAPI для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (JsonException ex)
    {
        // Ошибка парсинга – логируем с телом ответа, если возможно
        _logger.LogError(ex, "Ошибка парсинга JSON от WeatherAPI для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Неожиданная ошибка при запросе к WeatherAPI для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
}
}