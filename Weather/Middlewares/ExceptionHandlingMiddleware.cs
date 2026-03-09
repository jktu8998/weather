using Weather.Exceptions;

namespace Weather.Middlewares;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

 public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env; // добавим, чтобы в разработке показывать детали

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Определяем статус-код и сообщение в зависимости от типа исключения
        int statusCode;
        string message;
        string? details = null;

        switch (exception)
        {
            case DomainException domainEx:
                statusCode = domainEx.StatusCode;
                message = domainEx.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                message = "Unauthorized";
                break;
            // потом можно добавить обработку других стандартных исключений, например, ArgumentException -> 400
            case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = argEx.Message;
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An internal server error occurred.";
                details = exception.Message; // в проде лучше не показывать
                break;
        }

        // Логируем с соответствующим уровнем
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with status {StatusCode}: {Message}", statusCode, exception.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Формируем ответ
        var response = new
        {
            error = message,
            details = _env.IsDevelopment() ? details : null // показываем детали только в разработке
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}