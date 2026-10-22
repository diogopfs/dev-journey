using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DevJourney.Infrastructure.HealthChecks;

internal sealed class MongoHealthCheck(IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB indisponível", ex);
        }
    }
}
