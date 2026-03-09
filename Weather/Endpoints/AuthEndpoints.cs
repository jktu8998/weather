namespace Weather.Endpoints;

using System.Security.Claims;
//using Microsoft.AspNetCore.Identity.Data;
using Weather.Interfaces;
using System.ComponentModel.DataAnnotations;
using Weather.DTOs.Auth;
using Weather.Services.Auth;


public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegistrRequest request, IAuthService authService) =>
            {
                await authService.RegisterAsync(request);
                return Results.Ok(new { message = "Registration successful" });
            })
            .WithName("Register")
            .WithOpenApi();

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
            {
                var result = await authService.LoginAsync(request);
                return Results.Ok(result);
            })
            .WithName("Login")
            .WithOpenApi();

        group.MapPost("/refresh", async (RefreshRequest request, IAuthService authService) =>
            {
                var result = await authService.RefreshTokenAsync(request.RefreshToken);
                return Results.Ok(result);
            })
            .WithName("Refresh")
            .WithOpenApi();
    
        //var meGroup = group.MapGroup("/me").RequireAuthorization();

        group.MapPost("/update", async (ChangePasswordRequest request, IAuthService authService, HttpContext httpContext) =>
            {
                var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized(); // это исключение не будет выброшено сервисом 

                await authService.ChangePasswordAsync(userId, request);
                return Results.Ok(new { message = "Password changed successfully" });
            })
            .RequireAuthorization()  
            .WithName("ChangePassword")
            .WithOpenApi();

        group.MapPost("/delete", async (DeleteAccountRequest request, IAuthService authService, HttpContext httpContext) =>
            {
                var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                await authService.DeleteAccountAsync(userId, request.Password);
                return Results.Ok(new { message="User deleted successfully"});
            })
            .WithName("DeleteAccount")
            .WithOpenApi();
    }
}