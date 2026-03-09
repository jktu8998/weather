using System.Diagnostics;

namespace Weather.Services.WeatherService;

using Weather.Interfaces;
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
    

    // 2. Экранирование и формирование URL
    var encodedCity = Uri.EscapeDataString(city);
    var url = $"{encodedCity}?format=j1"; // базовый адрес уже установлен в HttpClient

    var stopwatch = Stopwatch.StartNew();

    try
    {
        _logger.LogInformation("Запрос к wttr.in для города {City}", city);

        // 3. Выполнение запроса
        var response = await _httpClient.GetAsync(url, cancellationToken);

        var httpElapsed = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart(); // для замера парсинга

        // 4. Обработка неуспешного статуса
        if (!response.IsSuccessStatusCode)
        {
            string? responseBody = null;
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            _logger.LogWarning("wttr.in вернул статус {StatusCode} для города {City}. Тело: {ResponseBody}",
                (int)response.StatusCode, city, responseBody ?? "(не удалось прочитать)");
            return null;
        }

        // 5. Чтение и парсинг JSON
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Проверяем наличие current_condition и его первого элемента
        if (!root.TryGetProperty("current_condition", out var currentConditionArray) ||
            currentConditionArray.ValueKind != JsonValueKind.Array ||
            currentConditionArray.GetArrayLength() == 0)
        {
            _logger.LogWarning("Ответ wttr.in не содержит current_condition для города {City}", city);
            return null;
        }

        var currentCondition = currentConditionArray[0];

        // Безопасное извлечение температуры (строковое поле)
        if (!TryGetDoubleFromString(currentCondition, "temp_C", out var temp))
        {
            _logger.LogWarning("Не удалось распарсить температуру в ответе wttr.in для города {City}", city);
            return null;
        }

        // Безопасное извлечение скорости ветра (строковое поле)
        if (!TryGetDoubleFromString(currentCondition, "windspeedKmph", out var windSpeed))
        {
            _logger.LogWarning("Не удалось распарсить скорость ветра в ответе wttr.in для города {City}", city);
            return null;
        }

        // Извлечение описания погоды
        string condition = "Unknown";
        if (currentCondition.TryGetProperty("weatherDesc", out var weatherDescArray) &&
            weatherDescArray.ValueKind == JsonValueKind.Array &&
            weatherDescArray.GetArrayLength() > 0 &&
            weatherDescArray[0].TryGetProperty("value", out var valueProp))
        {
            condition = valueProp.GetString() ?? "Unknown";
        }

        var parseElapsed = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation("wttr.in успешно ответил для города {City}. HTTP: {HttpMs} мс, парсинг: {ParseMs} мс, всего: {TotalMs} мс",
            city, httpElapsed, parseElapsed, httpElapsed + parseElapsed);

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
        _logger.LogDebug("Запрос к wttr.in для города {City} отменён", city);
        return null;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Сетевая ошибка при запросе к wttr.in для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Ошибка парсинга JSON от wttr.in для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Неожиданная ошибка при запросе к wttr.in для города {City} после {ElapsedMs} мс",
            city, stopwatch.ElapsedMilliseconds);
        return null;
    }
}

// Вспомогательный метод для безопасного парсинга строкового числа
private bool TryGetDoubleFromString(JsonElement element, string propertyName, out double value)
{
    value = 0;
    if (element.TryGetProperty(propertyName, out var prop) &&
        prop.ValueKind == JsonValueKind.String)
    {
        var str = prop.GetString();
        if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
        {
            return true;
        }
    }
    return false;
}
}