using Echo.Models;

namespace Echo.Services.Abstractions;

public interface IUserService
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string username, string password, CancellationToken cancellationToken = default);
}