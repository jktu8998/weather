using Weather.Configurations;
using Weather.Data;
using Weather.Interfaces;
using Weather.Models;

namespace Weather.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
 

 
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly AppDbContext _context;

    public TokenService(JwtSettings jwtSettings, AppDbContext context)
    {
        _jwtSettings = jwtSettings;
        _context = context;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
            // можно добавить роли и т.д.
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<RefreshToken> StoreRefreshTokenAsync(int userId, string refreshToken)
    {
        var token = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);

        if (token == null || token.IsRevoked || token.ExpiryDate < DateTime.UtcNow)
            return false;

        return true;
    }
    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (token != null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }
    public async Task RevokeAllUserRefreshTokensAsync(int userId)
    {
        var tokens = _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked);
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }
        await _context.SaveChangesAsync();
    }
}