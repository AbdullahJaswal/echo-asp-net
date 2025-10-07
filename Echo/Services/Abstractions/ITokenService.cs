using Echo.Models;

namespace Echo.Services.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(User user);

    Task<(string refreshToken, DateTime expiresUtc)> CreateAndStoreRefreshTokenAsync(
        User user,
        CancellationToken cancellationToken = default
    );

    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<User?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
