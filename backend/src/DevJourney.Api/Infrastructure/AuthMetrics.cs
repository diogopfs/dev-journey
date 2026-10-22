using System.Diagnostics.Metrics;
using DevJourney.Infrastructure.Observability;

namespace DevJourney.Api.Infrastructure;

/// <summary>
/// Métrica de negócio customizada: tentativas de login por resultado.
/// Útil, por exemplo, para criar alertas de ataques de força bruta.
/// </summary>
public sealed class AuthMetrics
{
    private readonly Counter<long> _logins;

    public AuthMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ObservabilityExtensions.AppMeterName);
        _logins = meter.CreateCounter<long>("devjourney.auth.logins", description: "Tentativas de login");
    }

    public void RecordLogin(bool success) =>
        _logins.Add(1, new KeyValuePair<string, object?>("result", success ? "success" : "failure"));
}
