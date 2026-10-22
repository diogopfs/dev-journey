using DevJourney.Application.Abstractions.Security;
using DevJourney.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevJourney.Infrastructure.Persistence;

/// <summary>
/// Aplica as migrations pendentes e cria o usuário administrador inicial (seed).
/// Em produção real, migrations costumam rodar no pipeline de deploy, não no startup.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var config = sp.GetRequiredService<IConfiguration>();

        logger.LogInformation("Aplicando migrations do banco relacional...");
        await db.Database.MigrateAsync(ct);

        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) return;

        var email = Email.Create(adminEmail);
        if (await db.Users.AnyAsync(u => u.Email.Value == email.Value, ct)) return;

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var time = sp.GetRequiredService<TimeProvider>();
        db.Users.Add(User.Create("Administrador", email, hasher.Hash(adminPassword), UserRole.Admin, time.GetUtcNow()));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Usuário administrador {Email} criado.", email);
    }
}
