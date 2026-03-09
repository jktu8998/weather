using System.Text.RegularExpressions;
using Weather.Extensions;
using Weather.Interfaces;

namespace Weather.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Weather.DTOs.Weather;
using Weather.Services.WeatherService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

 
public static class WeatherEndpoints
{
    public static void MapWeatherEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/weather")
            .RequireAuthorization() // защита JWT
            .WithTags("Weather");

        group.MapGet("/{city}", async (string city, [FromServices] IWeatherAggregator aggregator,
        [FromServices]ILogger<Program> logger) =>
            {
                if (!CityNameValidator.IsValid(city, out var error))
                    return Results.BadRequest(new { Error = error });
                
                logger.LogInformation("Получен GET-запрос для города {City}", city);
                var result = await aggregator.GetWeatherWithCacheAsync(city);
                if (result == null)
                {
                    logger.LogWarning("Данные не найдены для города {City}", city);
                    return Results.NotFound("No weather data found");
                }
                return Results.Ok(result);            })
            .WithName("GetWeather")
            .WithOpenApi();

         group.MapPost("/compare", async (
                [FromBody] CompareRequest request,
                CancellationToken cancellationToken,
                [FromServices] IWeatherComparisonService comparisonService,
                [FromServices] ILogger<Program> logger) =>
            {
                // Ручная проверка количества городов (можно оставить, если не используем ModelState)
                if (request.Cities.Count != 2)
                {
                    logger.LogWarning("Некорректное количество городов для сравнения: {Count}", request.Cities.Count);
                    return Results.BadRequest(new { Error = "Please provide exactly two cities" });
                }

                // Валидация каждого названия города
                foreach (var city in request.Cities)
                {
                    if (!CityNameValidator.IsValid(city, out var cityError))
                    {
                        logger.LogWarning("Невалидное имя города в запросе: {City}, ошибка: {Error}", city, cityError);
                        return Results.BadRequest(new { Error = $"Invalid city name '{city}': {cityError}" });
                    }
                }

                logger.LogInformation("Запрос на сравнение городов: {City1} и {City2}", 
                    request.Cities[0], request.Cities[1]);

                var result = await comparisonService.CompareAsync(
                    request.Cities[0], 
                    request.Cities[1], 
                    cancellationToken);

                if (result == null)
                {
                    return Results.NotFound(new { Error = "Weather data not found for one or both cities" });
                }

                return Results.Ok(result);
            })
            .WithName("CompareWeather")
            .WithOpenApi();
    }
}