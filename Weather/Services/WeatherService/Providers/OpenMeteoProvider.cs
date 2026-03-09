using System.Diagnostics;

namespace Weather.Services.WeatherService;

using Weather.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Weather.Models;
 
 
public class OpenMeteoProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<OpenMeteoProvider> _logger;
    
    
    public string ProviderName => "Open-Meteo";

    public OpenMeteoProvider(
        HttpClient httpClient,
        IGeocodingService geocodingService,
        ILogger<OpenMeteoProvider> logger)
    {
        _httpClient = httpClient;
        _geocodingService = geocodingService;
        _logger = logger;
    }

   public async Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
{
    // 1. Минимальная защита от пустого города
    if (string.IsNullOrWhiteSpace(city))
    {
        _logger.LogWarning("GetWeatherAsync вызван с пустым городом");
        return null;
    }

    var totalStopwatch = Stopwatch.StartNew();
    TimeSpan? geoElapsed = null;
    TimeSpan? weatherApiElapsed = null;

    try
    {
        // Шаг 1: геокодинг
        _logger.LogInformation("Запрос координат для города {City} через геокодинг", city);

        var geoStopwatch = Stopwatch.StartNew();
        var geoResult = await _geocodingService.GetCoordinatesAsync(city, cancellationToken);
        geoStopwatch.Stop();
        geoElapsed = geoStopwatch.Elapsed;

        if (geoResult == null)
        {
            _logger.LogWarning("Не удалось определить координаты для города {City} (геокодинг занял {GeoMs} мс)",
                city, geoElapsed.Value.TotalMilliseconds);
            return null;
        }

        _logger.LogInformation("Геокодинг для города {City} выполнен за {GeoMs} мс. Найден: {DisplayName} ({Lat}, {Lon})",
            city, geoElapsed.Value.TotalMilliseconds, geoResult.DisplayName, geoResult.Latitude, geoResult.Longitude);

        // Шаг 2: запрос погоды по координатам
        var weatherStopwatch = Stopwatch.StartNew();
        var url = $"forecast?latitude={geoResult.Latitude}&longitude={geoResult.Longitude}" +
                 $"&current_weather=true" +
                 $"&hourly=temperature_2m,wind_speed_10m,weathercode" +
                 $"&timezone={Uri.EscapeDataString(geoResult.Timezone)}" +
                 $"&windspeed_unit=kmh";

        _logger.LogDebug("Запрос к Open-Meteo Weather API: {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);

        // Замер времени выполнения HTTP запроса
        var httpElapsed = weatherStopwatch.ElapsedMilliseconds;
        weatherStopwatch.Restart(); // для замера парсинга

        // Обработка неуспешного статуса
        if (!response.IsSuccessStatusCode)
        {
            string? responseBody = null;
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            _logger.LogWarning("Open-Meteo Weather API вернул статус {StatusCode} для города {City}. Тело: {ResponseBody}",
                (int)response.StatusCode, city, responseBody ?? "(не удалось прочитать)");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Проверяем наличие current_weather
        if (!root.TryGetProperty("current_weather", out var currentWeather))
        {
            _logger.LogWarning("Ответ Open-Meteo Weather API не содержит current_weather для города {City}", city);
            return null;
        }

        // Безопасное извлечение полей
        if (!currentWeather.TryGetProperty("temperature", out var tempProp) || tempProp.ValueKind != JsonValueKind.Number)
        {
            _logger.LogWarning("Отсутствует или некорректное поле temperature в ответе Open-Meteo для города {City}", city);
            return null;
        }
        var temperature = tempProp.GetDouble();

        if (!currentWeather.TryGetProperty("windspeed", out var windProp) || windProp.ValueKind != JsonValueKind.Number)
        {
            _logger.LogWarning("Отсутствует или некорректное поле windspeed в ответе Open-Meteo для города {City}", city);
            return null;
        }
        var windSpeed = windProp.GetDouble();

        if (!currentWeather.TryGetProperty("weathercode", out var codeProp) || codeProp.ValueKind != JsonValueKind.Number)
        {
            _logger.LogWarning("Отсутствует или некорректное поле weathercode в ответе Open-Meteo для города {City}", city);
            return null;
        }
        var weatherCode = codeProp.GetInt32();
        var condition = GetWeatherDescription(weatherCode);

        var parseElapsed = weatherStopwatch.ElapsedMilliseconds;
        weatherStopwatch.Stop();
        weatherApiElapsed = TimeSpan.FromMilliseconds(httpElapsed + parseElapsed);

        totalStopwatch.Stop();

        _logger.LogInformation(
            "Open-Meteo успешно ответил для города {City}. Геокодинг: {GeoMs} мс, HTTP: {HttpMs} мс, парсинг: {ParseMs} мс, всего: {TotalMs} мс",
            city,
            geoElapsed.Value.TotalMilliseconds,
            httpElapsed,
            parseElapsed,
            totalStopwatch.ElapsedMilliseconds);

        return new WeatherData
        {
            City = geoResult.Name, // Используем официальное название
            TemperatureC = temperature,
            WindSpeedKph = windSpeed,
            Condition = condition,
            Source = ProviderName
        };
    }
    catch (OperationCanceledException)
    {
        _logger.LogDebug("Запрос к Open-Meteo для города {City} отменён", city);
        return null;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Сетевая ошибка при запросе к Open-Meteo для города {City} после {ElapsedMs} мс",
            city, totalStopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Ошибка парсинга JSON от Open-Meteo для города {City} после {ElapsedMs} мс",
            city, totalStopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Неожиданная ошибка при запросе к Open-Meteo для города {City} после {ElapsedMs} мс",
            city, totalStopwatch.ElapsedMilliseconds);
        return null;
    }
}

    private string GetWeatherDescription(int code)
    {
        // WMO Weather interpretation codes (https://open-meteo.com/en/docs)
        return code switch
        {
            0 => "Ясно",
            1 => "Преимущественно ясно",
            2 => "Переменная облачность",
            3 => "Пасмурно",
            45 => "Туман",
            48 => "Изморозь",
            51 => "Легкая морось",
            53 => "Морось",
            55 => "Сильная морось",
            56 => "Легкий ледяной дождь",
            57 => "Ледяной дождь",
            61 => "Небольшой дождь",
            63 => "Дождь",
            65 => "Сильный дождь",
            66 => "Легкий ледяной дождь",
            67 => "Ледяной дождь",
            71 => "Небольшой снег",
            73 => "Снег",
            75 => "Сильный снег",
            77 => "Снежная крупа",
            80 => "Небольшой ливень",
            81 => "Ливень",
            82 => "Сильный ливень",
            85 => "Небольшой снегопад",
            86 => "Снегопад",
            95 => "Гроза",
            96 => "Гроза с градом",
            99 => "Сильная гроза с градом",
            _ => "Неизвестно"
        };
    }
}