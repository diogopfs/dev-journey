using DevJourney.Infrastructure.Messaging;
using DevJourney.Infrastructure.NoSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevJourney.Infrastructure.Observability;

/// <summary>
/// Configura os três pilares da observabilidade com OpenTelemetry (padrão aberto, independente de fornecedor):
/// - Traces  (o caminho de uma requisição entre serviços)
/// - Metrics (números agregados: req/s, latência, mensagens processadas...)
/// - Logs    (eventos estruturados, correlacionados ao trace)
/// Os dados são exportados via protocolo OTLP para qualquer backend compatível
/// (aqui usamos o .NET Aspire Dashboard; poderia ser Jaeger, Grafana, Datadog, etc.).
/// </summary>
public static class ObservabilityExtensions
{
    public const string AppMeterName = "DevJourney";

    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString())
                .AddAttributes([new("deployment.environment", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing => tracing
                .AddSource(MessagingTelemetry.SourceName)
                .AddSource(MongoAuditLogStore.ActivitySourceName)
                .AddAspNetCoreInstrumentation(o =>
                    // Não polui os traces com chamadas de health check.
                    o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter(AppMeterName)
                .AddMeter(MessagingTelemetry.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        // O exporter OTLP só é ligado se houver um endpoint configurado
        // (variável de ambiente padrão OTEL_EXPORTER_OTLP_ENDPOINT, ex.: http://localhost:4317).
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }
}
