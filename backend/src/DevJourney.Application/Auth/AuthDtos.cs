using DevJourney.Application.Users;

namespace DevJourney.Application.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

/// <summary>
/// Resultado de login/refresh. A API devolve o access token no corpo e o refresh token em cookie HttpOnly.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserResponse User);
