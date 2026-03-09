namespace Weather.Services.Auth;

using Weather.Configurations;
using Weather.Data;
using Weather.Interfaces;
using Weather.Models;
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

    public   Task<RefreshToken> StoreRefreshTokenAsync(int userId, string refreshToken)
    {
        var token = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(token);
        return   Task.FromResult(token);   // возвращаем завершённую задачу
    }

    
     public async Task RevokeRefreshTokenAsync(string refreshToken)
     {
         var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
         if (token != null)
         {
             token.IsRevoked = true; 
         }
     }
    public async Task RevokeAllUserRefreshTokensAsync(int userId)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true));
     }
}