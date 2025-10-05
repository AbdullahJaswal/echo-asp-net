using Echo.DTOs;
using Echo.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Echo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService users, IPasswordHasher hasher, ITokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var user = await users.FindByUsernameAsync(request.Username, cancellationToken);

        if (user == null) return Unauthorized();
        if (!user.IsActive) return Forbid();
        if (!hasher.Verify(request.Password, user.PasswordHash)) return Unauthorized();

        var access = tokens.CreateAccessToken(user);

        var (refresh, refreshExpiry) = await tokens.CreateAndStoreRefreshTokenAsync(
            user,
            cancellationToken
        );

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(access);

        return Ok(new TokenResponse
        {
            AccessToken = access,
            AccessTokenExpiresUtc = jwt.ValidTo,
            RefreshToken = refresh,
            RefreshTokenExpiresUtc = refreshExpiry
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        var username = request.Username.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || username.Length > 100)
            return BadRequest();

        var existing = await users.FindByUsernameAsync(username, cancellationToken);
        if (existing != null) return Conflict();

        try
        {
            var user = await users.CreateAsync(username, password, cancellationToken);

            var access = tokens.CreateAccessToken(user);
            var (refresh, refreshExpiry) = await tokens.CreateAndStoreRefreshTokenAsync(
                user,
                cancellationToken
            );

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(access);

            return Ok(new TokenResponse
            {
                AccessToken = access,
                AccessTokenExpiresUtc = jwt.ValidTo,
                RefreshToken = refresh,
                RefreshTokenExpiresUtc = refreshExpiry
            });
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken
    )
    {
        var user = await tokens.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user == null) return Unauthorized();

        await tokens.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken); // rotate

        var access = tokens.CreateAccessToken(user);

        var (refresh, rExp) = await tokens.CreateAndStoreRefreshTokenAsync(user, cancellationToken);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(access);

        return Ok(new TokenResponse
        {
            AccessToken = access,
            AccessTokenExpiresUtc = jwt.ValidTo,
            RefreshToken = refresh,
            RefreshTokenExpiresUtc = rExp
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return BadRequest();

        await tokens.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

        return NoContent();
    }
}