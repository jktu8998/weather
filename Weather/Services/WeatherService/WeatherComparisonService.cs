namespace Weather.Services.WeatherService;

using Microsoft.Extensions.Logging;
using Weather.DTOs.Weather;
using Weather.Interfaces;

 
public class WeatherComparisonService : IWeatherComparisonService
{
    private readonly IWeatherAggregator _aggregator;
    private readonly ILogger<WeatherComparisonService> _logger;

    public WeatherComparisonService(IWeatherAggregator aggregator, ILogger<WeatherComparisonService> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task<CompareResponse?> CompareAsync(string city1, string city2, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Сравнение погоды для городов {City1} и {City2}", city1, city2);

        // Получаем данные через агрегатор (с кэшем)
        var data1 = await _aggregator.GetWeatherWithCacheAsync(city1, cancellationToken);
        var data2 = await _aggregator.GetWeatherWithCacheAsync(city2, cancellationToken);

        if (data1 == null || data2 == null)
        {
            var notFound = data1 == null ? city1 : city2;
            _logger.LogWarning("Не удалось получить данные для города {City}", notFound);
            return null;
        }

        // Вычисляем разницы
        var tempDiff = Math.Abs(data1.Average.TemperatureC - data2.Average.TemperatureC);
        var windDiff = Math.Abs(data1.Average.WindSpeedKph - data2.Average.WindSpeedKph);
        var warmerCity = data1.Average.TemperatureC > data2.Average.TemperatureC ? data1.City : data2.City;
        var lessWindyCity = data1.Average.WindSpeedKph < data2.Average.WindSpeedKph ? data1.City : data2.City;

        // Формируем читаемое описание
        var summary = $"В городе {data1.City} температура {data1.Average.TemperatureC:F1}°C, ветер {data1.Average.WindSpeedKph:F1} км/ч; " +
                      $"в городе {data2.City} температура {data2.Average.TemperatureC:F1}°C, ветер {data2.Average.WindSpeedKph:F1} км/ч. " +
                      $"Температура выше в {warmerCity} на {tempDiff:F1}°C. " +
                      $"Ветер слабее в {lessWindyCity} на {windDiff:F1} км/ч.";

        var comparison = new ComparisonResult
        {
            TemperatureDifference = tempDiff,
            WarmerCity = warmerCity,
            WindDifference = windDiff,
            LessWindyCity = lessWindyCity,
            Summary = summary
        };

        return new CompareResponse
        {
            City1 = data1,
            City2 = data2,
            Comparison = comparison
        };
    }
}