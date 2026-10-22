using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DevJourney.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(User user)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role.ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        // 64 bytes aleatórios criptograficamente seguros -> token opaco (não é JWT).
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = timeProvider.GetUtcNow().AddDays(_options.RefreshTokenDays);
        return new GeneratedRefreshToken(token, HashRefreshToken(token), expiresAt);
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
