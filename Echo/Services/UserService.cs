using Echo.Data;
using Echo.Models;
using Echo.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Echo.Services;

public sealed class UserService(AppDbContext db, IPasswordHasher hasher) : IUserService
{
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await db.Users.SingleOrDefaultAsync(user => user.Username == username, cancellationToken);
    }

    public async Task<User> CreateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        var exists = await db.Users.AnyAsync(user => user.Username == username, cancellationToken);

        if (exists) throw new InvalidOperationException("Username already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = hasher.Hash(password),
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user;
    }
}