using Weather.Interfaces;

namespace Weather.Services.WeatherService;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Weather.Models;
using Weather.Services.Geocoding;

 
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
        try
        {
            // Шаг 1: получаем координаты через сервис геокодинга
            var geoResult = await _geocodingService.GetCoordinatesAsync(city, cancellationToken);
            if (geoResult == null)
            {
                _logger.LogWarning("Не удалось определить координаты для города {City}", city);
                return null;
            }

            _logger.LogInformation("Найден город {City} -> {DisplayName} ({Lat}, {Lon})", 
                city, geoResult.DisplayName, geoResult.Latitude, geoResult.Longitude);

            // Шаг 2: запрашиваем погоду по координатам
            var url = $"forecast?latitude={geoResult.Latitude}&longitude={geoResult.Longitude}" +
                     $"&current_weather=true" +
                     $"&hourly=temperature_2m,wind_speed_10m,weathercode" +
                     $"&timezone={Uri.EscapeDataString(geoResult.Timezone)}" +
                     $"&windspeed_unit=kmh"; // просим ветер в км/ч

            _logger.LogDebug("Запрос к Open-Meteo Weather API: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API вернул статус {StatusCode} для города {City}", 
                    response.StatusCode, city);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // current_weather - объект с текущей погодой
            if (!root.TryGetProperty("current_weather", out var currentWeather))
            {
                _logger.LogWarning("Ответ Weather API не содержит current_weather для города {City}", city);
                return null;
            }

            var temperature = currentWeather.GetProperty("temperature").GetDouble();
            var windSpeed = currentWeather.GetProperty("windspeed").GetDouble();
            var weatherCode = currentWeather.GetProperty("weathercode").GetInt32();
            var condition = GetWeatherDescription(weatherCode);

            _logger.LogInformation("Успешно получены данные от Open-Meteo для города {City}: {Temp}°C, ветер {Wind} км/ч, {Condition}", 
                city, temperature, windSpeed, condition);

            return new WeatherData
            {
                City = geoResult.Name, // Используем официальное название
                TemperatureC = temperature,
                WindSpeedKph = windSpeed,
                Condition = condition,
                Source = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении погоды от Open-Meteo для города {City}", city);
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