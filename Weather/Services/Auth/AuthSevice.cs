using Weather.Exceptions;

namespace Weather.Services.Auth;

using Weather.Configurations;
using Weather.Data;
using Weather.DTOs.Auth;
using Weather.Interfaces;
using Weather.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
 

 
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(AppDbContext context, ITokenService tokenService, JwtSettings jwtSettings,ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _tokenService = tokenService;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public async Task  RegisterAsync( RegistrRequest request)
    {
        _logger.LogInformation("Попытка регистрации пользователя с email {Email}", request.Email);
        // Проверяем, не занят ли email
        // проверка существования
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            throw new ConflictException("User with this email already exists.");
        }

        // Создаём пользователя
        var user = new User { Email = request.Email };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        
        _context.Users.Add(user);
        _logger.LogInformation("Пользователь {Email} успешно зарегистрирован (Id: {UserId})", request.Email, user.Id);
        await _context.SaveChangesAsync();
    }
    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Попытка входа пользователя с email {Email}", request.Email);
        // 1. Ищем пользователя по email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            _logger.LogWarning("Вход отклонён: пользователь с email {Email} не найден", request.Email);
           throw new UnauthorizedException("Invalid email ");
        }
        
        // 2. Проверяем пароль
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Вход отклонён: неверный пароль для email {Email}", request.Email);
            throw new UnauthorizedException("Invalid   password");
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
    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Refresh attempt with token: {RefreshToken}", refreshToken);
        var tokenEntity = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
         if (tokenEntity == null)
         {
                    _logger.LogWarning("Refresh token не найден");
                    throw new UnauthorizedException("Invalid refresh token");
         }
        _logger.LogInformation("Token found: UserId={UserId}, Expiry={Expiry}, IsRevoked={IsRevoked}",
            tokenEntity.UserId, tokenEntity.ExpiryDate, tokenEntity.IsRevoked);
        if (tokenEntity.IsRevoked || tokenEntity.ExpiryDate < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token отозван или истёк для пользователя {UserId}", tokenEntity.UserId);
            throw new UnauthorizedException("Invalid refresh token");
        }
        // 3. Отзываем старый токен (однократное использование)
        tokenEntity.IsRevoked = true;
        // 4. Генерируем новую пару
        var newAccessToken = _tokenService.GenerateAccessToken(tokenEntity.User);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        // 5. Сохраняем новый refresh token в БД
        await _tokenService.StoreRefreshTokenAsync(tokenEntity.UserId, newRefreshToken);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Токены успешно обновлены для пользователя {UserId}", tokenEntity.UserId);
        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresIn = _jwtSettings.AccessTokenLifetimeMinutes * 60
        };
    }
    public async Task  ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        _logger.LogInformation("User {UserId} attempting to change password", userId);
    
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for password change", userId);
            throw new NotFoundException("User not found");
        }
    
        // Проверяем текущий пароль
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("User {UserId} provided incorrect current password", userId);
            throw new UnauthorizedException("Current password is incorrect");
        }
    
        // Хешируем новый пароль
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        _context.Users.Update(user);
        // Опционально: отзываем все refresh-токены пользователя, чтобы старые сессии не работали
        await _tokenService.RevokeAllUserRefreshTokensAsync(userId);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Password changed successfully for user {UserId}", userId);
     }

     public async Task  DeleteAccountAsync(int userId, string password)
        {
            _logger.LogInformation("User {UserId} attempting to delete account", userId);
        
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for deletion", userId);
                throw new NotFoundException("User not found");
            }
        
            // Проверяем пароль
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("User {UserId} provided incorrect password for account deletion", userId);
                throw new UnauthorizedException("Current password is incorrect");
            }
        
            // Удаляем все связанные refresh-токены  
            var refreshTokens = _context.RefreshTokens.Where(rt => rt.UserId == userId);
            _context.RefreshTokens.RemoveRange(refreshTokens);
        
            // Удаляем пользователя
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        
            _logger.LogInformation("Account deleted for user {UserId}", userId);
          }
}