using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Domain.Auth;
using DevJourney.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevJourney.Infrastructure.Persistence;

/// <summary>
/// DbContext do EF Core. Também atua como Unit of Work: SaveChangesAsync confirma tudo em uma transação.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        // Aplica todas as classes IEntityTypeConfiguration deste assembly (Configurations/*).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
