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
    }
}