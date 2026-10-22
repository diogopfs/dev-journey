using DevJourney.Domain.Users;

namespace DevJourney.Application.Abstractions.Persistence;

/// <summary>
/// Porta (interface) de acesso a dados de usuários.
/// A camada de Application depende da abstração; a Infrastructure fornece a implementação
/// (Dependency Inversion Principle - o "D" do SOLID).
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(Email email, Guid? ignoreUserId = null, CancellationToken ct = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    void Add(User user);
    void Remove(User user);
}
