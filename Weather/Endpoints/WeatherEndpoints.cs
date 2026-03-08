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
                if (string.IsNullOrWhiteSpace(city))
                    return Results.BadRequest("City is required");
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

        group.MapPost("/compare", async (CompareRequest request,[FromServices] IWeatherAggregator aggregator) =>
            {
                if (request.Cities.Count != 2)
                    return Results.BadRequest("Please provide exactly two cities");

                var city1 = request.Cities[0];
                var city2 = request.Cities[1];

                var result1 = await aggregator.GetWeatherWithCacheAsync(city1);
                var result2 = await aggregator.GetWeatherWithCacheAsync(city2);

                if (result1 == null || result2 == null)
                    return Results.NotFound("One or both cities not found");

                return Results.Ok(new { City1 = result1, City2 = result2 });
            })
            .WithName("CompareWeather")
            .WithOpenApi();
    }
}