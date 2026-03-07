using Weather.Configurations;
using Weather.Data;
using Weather.DTOs.Auth;
using Weather.Interfaces;
using Weather.Models;

namespace Weather.Services;

 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
 

 
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    
    public AuthService(AppDbContext context, ITokenService tokenService, JwtSettings jwtSettings)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _tokenService = tokenService;
        _jwtSettings = jwtSettings;
    }

    public async Task<AuthResult> RegisterAsync( RegistrRequest request)
    {
        // Проверяем, не занят ли email
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return new AuthResult { Success = false, Message = "User with this email already exists." };
        }

        // Создаём пользователя
        var user = new User { Email = request.Email };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new AuthResult { Success = true, Message = "Registration successful" };
    }
    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        // 1. Ищем пользователя по email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // 2. Проверяем пароль
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // 3. Генерируем access token
        var accessToken = _tokenService.GenerateAccessToken(user);

        // 4. Генерируем refresh token и сохраняем в БД
        var refreshTokenString = _tokenService.GenerateRefreshToken();
        await _tokenService.StoreRefreshTokenAsync(user.Id, refreshTokenString);

        // 5. Возвращаем ответ
        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            AccessTokenExpiresIn = _jwtSettings.AccessTokenLifetimeMinutes * 60
        };
    }
}