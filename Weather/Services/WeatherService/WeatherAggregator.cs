using Weather.Interfaces;

namespace Weather.Services.WeatherService;

using Microsoft.Extensions.Caching.Memory;
using Weather.DTOs.Weather;

 
public class WeatherAggregator : IWeatherAggregator
{
    private readonly IEnumerable<IWeatherProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);
    private readonly ILogger<WeatherAggregator> _logger;

    public WeatherAggregator(IEnumerable<IWeatherProvider> providers, 
                             IMemoryCache cache,ILogger<WeatherAggregator> logger )            
    {
        _providers = providers;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WeatherResponse?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        // Опрашиваем все провайдеры параллельно
        _logger.LogInformation("Запрос погоды для города {City} от всех провайдеров", city);
        var tasks = _providers.Select(p => p.GetWeatherAsync(city, cancellationToken));
        var results = await Task.WhenAll(tasks);

        var validResults = results.Where(r => r != null).ToList();
        _logger.LogInformation("Получено {Count} успешных ответов для города {City}", validResults.Count, city);
        if (!validResults.Any())
        {
            _logger.LogWarning("Ни один провайдер не вернул данные для города {City}", city);
            return null;
        }

        // Усредняем
        var avgTemp = validResults.Average(r => r.TemperatureC);
        var avgWind = validResults.Average(r => r.WindSpeedKph);
        // Для условия можно взять наиболее часто встречающееся или просто первое
        var condition = validResults.GroupBy(r => r.Condition)
                                     .OrderByDescending(g => g.Count())
                                     .First().Key;

        var details = validResults.Select(r => new SourceDetail
        {
            Source = r.Source,
            TemperatureC = r.TemperatureC,
            WindSpeedKph = r.WindSpeedKph,
            Condition = r.Condition
        }).ToList();

        return new WeatherResponse
        {
            City = city,
            Average = new WeatherSummary
            {
                TemperatureC = avgTemp,
                WindSpeedKph = avgWind,
                Condition = condition
            },
            Details = details
        };
    }

    public async Task<WeatherResponse?> GetWeatherWithCacheAsync(string city, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"weather_{city.ToLower()}";
        _logger.LogDebug("Попытка получить данные из кеша по ключу {CacheKey}", cacheKey);
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            _logger.LogInformation("Кеш для города {City} не найден или устарел, запрашиваем свежие данные", city);
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            return await GetWeatherAsync(city, cancellationToken);
        });
    }
}