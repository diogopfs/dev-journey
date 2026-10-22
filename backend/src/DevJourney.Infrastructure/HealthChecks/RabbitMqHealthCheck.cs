using DevJourney.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevJourney.Infrastructure.HealthChecks;

internal sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var conn = await connection.GetConnectionAsync(ct);
            return conn.IsOpen ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Conexão fechada");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ indisponível", ex);
        }
    }
}
