using System.Diagnostics;
using System.Security.Claims;

namespace Weather.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var query = context.Request.QueryString.Value;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs} ms (User: {UserId})",
                method, path, query, statusCode, elapsedMs, userId);
        }
    }
}