using DevJourney.Domain.Users;

namespace DevJourney.Application.Abstractions.Security;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <param name="Token">Valor enviado ao cliente (nunca é salvo no banco).</param>
/// <param name="TokenHash">Hash SHA-256 do token, que é o que persistimos.</param>
public sealed record GeneratedRefreshToken(string Token, string TokenHash, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    GeneratedRefreshToken CreateRefreshToken();
    string HashRefreshToken(string token);
}
