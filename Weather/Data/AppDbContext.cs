using Microsoft.EntityFrameworkCore;
using Weather.Models;

namespace Weather.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; } 
    public DbSet<RefreshToken> RefreshTokens { get; set; }
     
}