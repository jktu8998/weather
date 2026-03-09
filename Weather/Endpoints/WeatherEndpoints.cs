using Weather.Extensions;
using Weather.Interfaces;
using Weather.DTOs.Weather;
using Microsoft.AspNetCore.Mvc;

namespace Weather.Endpoints;

public static class WeatherEndpoints
{
    public static void MapWeatherEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/weather")
            .RequireAuthorization()
            .WithTags("Weather");

        // GET /api/weather/{city}
        group.MapGet("/{city}", async (
                string city,
                CancellationToken cancellationToken,
                [FromServices] IWeatherAggregator aggregator,
                [FromServices] IGeocodingService geocodingService,
                [FromServices] ILogger<Program> logger) =>
            {
                 if (!CityNameValidator.IsValid(city, out var validationError))
                {
                    logger.LogWarning("Невалидное имя города: {City}, ошибка: {Error}", city, validationError);
                    return Results.BadRequest(new { Error = validationError });
                }

                //  Проверка существования города через геокодинг
                var geoResult = await geocodingService.GetCoordinatesAsync(city, cancellationToken);
                if (geoResult == null)
                {
                    logger.LogWarning("Город {City} не найден в геокодинге", city);
                    return Results.NotFound(new { Error = $"City '{city}' not found" });
                }

                 var officialCity = geoResult.Name;

                logger.LogInformation("Получен GET-запрос для города {City} (официально: {Official})", city, officialCity);

                //  Запрос погоды
                var weather = await aggregator.GetWeatherWithCacheAsync(officialCity, cancellationToken);
                if (weather == null)
                {
                    logger.LogWarning("Данные погоды не найдены для города {City}", officialCity);
                    return Results.NotFound(new { Error = "No weather data found" });
                }

                return Results.Ok(weather);
            })
            .WithName("GetWeather")
            .WithOpenApi();

         group.MapPost("/compare", async (
                [FromBody] CompareRequest request,
                CancellationToken cancellationToken,
                [FromServices] IWeatherComparisonService comparisonService,
                [FromServices] IGeocodingService geocodingService,
                [FromServices] ILogger<Program> logger) =>
            {
                 if (request.Cities.Count != 2)
                {
                    logger.LogWarning("Некорректное количество городов: {Count}", request.Cities.Count);
                    return Results.BadRequest(new { Error = "Exactly two cities are required" });
                }

                var city1 = request.Cities[0];
                var city2 = request.Cities[1];

                // Валидация формата каждого города
                foreach (var city in request.Cities)
                {
                    if (!CityNameValidator.IsValid(city, out var cityError))
                    {
                        logger.LogWarning("Невалидное имя города: {City}, ошибка: {Error}", city, cityError);
                        return Results.BadRequest(new { Error = $"Invalid city name '{city}': {cityError}" });
                    }
                }

                // Параллельная проверка существования городов через геокодинг
                var geoTask1 = geocodingService.GetCoordinatesAsync(city1, cancellationToken);
                var geoTask2 = geocodingService.GetCoordinatesAsync(city2, cancellationToken);
                await Task.WhenAll(geoTask1, geoTask2);

                var geo1 = await geoTask1;
                var geo2 = await geoTask2;

                if (geo1 == null)
                {
                    logger.LogWarning("Город {City} не найден в геокодинге", city1);
                    return Results.NotFound(new { Error = $"City '{city1}' not found" });
                }
                if (geo2 == null)
                {
                    logger.LogWarning("Город {City} не найден в геокодинге", city2);
                    return Results.NotFound(new { Error = $"City '{city2}' not found" });
                }

                // Используем официальные названия
                var officialCity1 = geo1.Name;
                var officialCity2 = geo2.Name;

                logger.LogInformation("Запрос на сравнение городов: {City1} -> {Official1}, {City2} -> {Official2}",
                    city1, officialCity1, city2, officialCity2);

                // Вызываем сервис сравнения  
                var result = await comparisonService.CompareAsync(officialCity1, officialCity2, cancellationToken);

                if (result == null)
                {
                    logger.LogWarning("Не удалось получить данные для сравнения городов {City1} и {City2}",
                        officialCity1, officialCity2);
                    return Results.NotFound(new { Error = "Weather data not found for one or both cities" });
                }

                return Results.Ok(result);
            })
            .WithName("CompareWeather")
            .WithOpenApi();
    }
}