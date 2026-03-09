using Microsoft.Extensions.Caching.Memory;
using Weather.Interfaces;
using Weather.Models;

namespace Weather.Services.Geocoding;

using System.Text.Json;
using Microsoft.Extensions.Logging;
 
 
public class OpenMeteoGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenMeteoGeocodingService> _logger;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(7);  



    public OpenMeteoGeocodingService(HttpClient httpClient, ILogger<OpenMeteoGeocodingService> logger,IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<GeocodingResult?> GetCoordinatesAsync(string city, CancellationToken cancellationToken = default)
    {

        try
        {
            var cacheKey = $"geocode_{city.ToLower()}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                // URL encode для безопасности
                var encodedCity = Uri.EscapeDataString(city);
                // Запрашиваем один результат, на русском языке, в формате JSON
                var url = $"search?name={encodedCity}&count=1&language=ru&format=json";

                _logger.LogDebug("Запрос к Open-Meteo Geocoding API для города {City}: {Url}", city, url);

                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Geocoding API вернул статус {StatusCode} для города {City}",
                        response.StatusCode, city);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                // Проверяем наличие поля "results"
                if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                {
                    _logger.LogInformation("Город {City} не найден в Geocoding API", city);
                    return null;
                }

                var first = results[0];

                // Извлекаем данные
                var latitude = first.GetProperty("latitude").GetDouble();
                var longitude = first.GetProperty("longitude").GetDouble();
                var name = first.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? city : city;

                // Пытаемся получить display_name  
                var country = first.TryGetProperty("country", out var countryProp) ? countryProp.GetString() ?? "" : "";
                var admin1 = first.TryGetProperty("admin1", out var admin1Prop) ? admin1Prop.GetString() ?? "" : "";
                var displayName = string.IsNullOrEmpty(admin1) ? $"{name}, {country}" : $"{name}, {admin1}, {country}";

                var timezone = first.TryGetProperty("timezone", out var tzProp) ? tzProp.GetString() ?? "auto" : "auto";

                return new GeocodingResult
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Name = name,
                    DisplayName = displayName,
                    Country = country,
                    Timezone = timezone
                };
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при вызове Geocoding API для города {City}", city);
            return null;
        } 
        
        }
        
        
}