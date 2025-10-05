namespace Echo.DTOs;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresUtc { get; set; }
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}