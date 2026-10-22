using DevJourney.Domain.Auth;

namespace DevJourney.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default);
    void Add(RefreshToken token);
}
