using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Echo.Data;
using Echo.Models;
using Echo.Options;
using Echo.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Echo.Services;

public sealed class TokenService : ITokenService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _opts;
    private readonly byte[] _keyBytes;

    public TokenService(AppDbContext db, IOptions<JwtOptions> options)
    {
        _db = db;
        _opts = options.Value;
        _keyBytes = Encoding.UTF8.GetBytes(_opts.Key);
    }

    public string CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("name", user.Username),
            new("preferred_username", user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64)
        };
        var algorithm = _keyBytes.Length >= 64
            ? SecurityAlgorithms.HmacSha512
            : SecurityAlgorithms.HmacSha256;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_keyBytes),
            algorithm
        );

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_opts.AccessTokenMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<(string refreshToken, DateTime expiresUtc)> CreateAndStoreRefreshTokenAsync(
        User user,
        CancellationToken cancellationToken = default
    )
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expires = DateTime.UtcNow.AddDays(_opts.RefreshTokenDays);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresUtc = expires,
            CreatedUtc = DateTime.UtcNow,
            Revoked = false
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return (token, expires);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var entity = await _db.RefreshTokens.SingleOrDefaultAsync(
            r => r.Token == refreshToken,
            cancellationToken
        );

        if (entity == null) return false;

        entity.Revoked = true;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<User?> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await _db.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.Token == refreshToken && !r.Revoked, cancellationToken);

        if (entity == null) return null;

        return entity.ExpiresUtc <= DateTime.UtcNow ? null : entity.User;
    }
}