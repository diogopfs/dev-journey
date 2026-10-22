using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevJourney.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email.Value == email.Value, ct);

    public Task<bool> EmailExistsAsync(Email email, Guid? ignoreUserId = null, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email.Value == email.Value && (ignoreUserId == null || u.Id != ignoreUserId), ct);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        // AsNoTracking: consultas somente leitura não precisam do change tracker (mais rápidas).
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(term) || u.Email.Value.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}
