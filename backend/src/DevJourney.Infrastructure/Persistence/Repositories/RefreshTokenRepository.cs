using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace DevJourney.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default) =>
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);
}
