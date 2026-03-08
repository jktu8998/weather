using System.Security.Claims;
using Microsoft.AspNetCore.Identity.Data;
using Weather.Interfaces;

namespace Weather.Endpoints;

using System.ComponentModel.DataAnnotations;
using Weather.DTOs.Auth;
using Weather.Services;


public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegistrRequest request, IAuthService authService) =>
            {
                // Валидация через DataAnnotations
                var validationContext = new ValidationContext(request);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    var errors = validationResults.Select(v => v.ErrorMessage);
                    return Results.BadRequest(new { Errors = errors });
                }

                var result = await authService.RegisterAsync(request);
                if (!result.Success)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { result.Message });
            })
            .WithName("Register")
            .WithOpenApi();

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
            {
                // Валидация
                var validationContext = new ValidationContext(request);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    var errors = validationResults.Select(v => v.ErrorMessage);
                    return Results.BadRequest(new { Errors = errors });
                }

                try
                {
                    var result = await authService.LoginAsync(request);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Unauthorized();
                }
            })
            .WithName("Login")
            .WithOpenApi();

        group.MapPost("/refresh", async (RefreshRequest request, IAuthService authService) =>
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return Results.BadRequest(new { Message = "Refresh token is required" });

                try
                {
                    var result = await authService.RefreshTokenAsync(request.RefreshToken);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
            })
            .WithName("Refresh")
            .WithOpenApi();
    
        //var meGroup = group.MapGroup("/me").RequireAuthorization();

        group.MapPost("/update", async (ChangePasswordRequest request, IAuthService authService, HttpContext httpContext) =>
            {
                // Извлекаем ID пользователя из JWT (ClaimTypes.NameIdentifier)
                var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var result = await authService.ChangePasswordAsync(userId, request);
                if (!result.Success)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { result.Message });
            })
            .WithName("ChangePassword")
            .WithOpenApi();

        group.MapPost("/delete", async (DeleteAccountRequest request, IAuthService authService, HttpContext httpContext) =>
            {
                var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var result = await authService.DeleteAccountAsync(userId, request.Password);
                if (!result.Success)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { result.Message });
            })
            .WithName("DeleteAccount")
            .WithOpenApi();
    }
}