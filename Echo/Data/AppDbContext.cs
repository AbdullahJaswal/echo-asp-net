using Echo.Models;
using Microsoft.EntityFrameworkCore;

namespace Echo.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(refreshToken => refreshToken.Token)
            .IsUnique();
    }
}