using Microsoft.Extensions.DependencyInjection;

namespace DevJourney.Infrastructure.NoSql;

public static class MongoInitializer
{
    public static Task InitializeMongoAsync(this IServiceProvider services, CancellationToken ct = default) =>
        services.GetRequiredService<MongoAuditLogStore>().EnsureIndexesAsync(ct);
}
