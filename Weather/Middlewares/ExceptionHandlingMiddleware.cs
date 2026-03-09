using Weather.Exceptions;

namespace Weather.Middlewares;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

 public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;  

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
             case ArgumentException argEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = argEx.Message;
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An internal server error occurred.";
                details = exception.Message;  
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

         var response = new
        {
            error = message,
            details = _env.IsDevelopment() ? details : null // показываем детали только в разработке
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}