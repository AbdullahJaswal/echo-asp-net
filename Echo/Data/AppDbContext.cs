using Echo.Models;

using Microsoft.EntityFrameworkCore;

namespace Echo.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);

            b.Property(u => u.Username)
                .HasMaxLength(100)
                .IsRequired();

            b.HasIndex(u => u.Username)
                .IsUnique();

            b.Property(u => u.CreatedUtc)
                .HasDefaultValueSql("GETUTCDATE()");

            b.Property(u => u.IsActive)
                .HasDefaultValue(true);

            b.HasMany(u => u.RoomMemberships)
                .WithOne(m => m.User)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasKey(rt => rt.Id);

            b.Property(rt => rt.Token)
                .HasMaxLength(512)
                .IsRequired();

            b.HasIndex(rt => rt.Token)
                .IsUnique();

            b.Property(rt => rt.CreatedUtc)
                .HasDefaultValueSql("GETUTCDATE()");

            b.Property(rt => rt.Revoked)
                .HasDefaultValue(false);

            b.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Room>(b =>
        {
            b.HasKey(r => r.Id);

            b.Property(r => r.Name)
                .HasMaxLength(100)
                .IsRequired();

            b.Property(r => r.CreatedUtc)
                .HasDefaultValueSql("GETUTCDATE()");

            b.Property(r => r.IsActive)
                .HasDefaultValue(true);

            b.HasMany(r => r.Members)
                .WithOne(m => m.Room)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomMember>(b =>
        {
            b.HasKey(m => new { m.RoomId, m.UserId });

            b.HasIndex(m => m.UserId);

            b.ToTable("RoomMembers");

            b.Property(m => m.JoinedUtc)
                .HasDefaultValueSql("GETUTCDATE()");

            b.Property(m => m.IsModerator)
                .HasDefaultValue(false);
        });
    }
}
