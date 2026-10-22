using DevJourney.Domain.Common;

namespace DevJourney.Domain.Auth;

/// <summary>
/// Refresh token persistido no banco. Guardamos apenas o HASH do token (nunca o valor em texto puro),
/// assim um vazamento do banco não permite reutilizar sessões.
/// Implementa "rotação": cada uso gera um novo token e revoga o anterior.
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        if (IsRevoked) return;
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
