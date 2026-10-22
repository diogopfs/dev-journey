using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace DevJourney.Infrastructure.Messaging;

/// <summary>
/// Instrumentação manual da mensageria:
/// - Traces: cria spans de "publish" e "process" e propaga o contexto (traceparent) nos headers da mensagem.
///   Assim, no dashboard, você vê UM trace atravessando API -> RabbitMQ -> Worker -> MongoDB.
/// - Métricas: contadores de mensagens publicadas, consumidas e com falha.
/// </summary>
public static class MessagingTelemetry
{
    public const string SourceName = "DevJourney.Messaging";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static readonly Counter<long> MessagesPublished =
        Meter.CreateCounter<long>("devjourney.messages.published", description: "Mensagens publicadas no broker");
    public static readonly Counter<long> MessagesConsumed =
        Meter.CreateCounter<long>("devjourney.messages.consumed", description: "Mensagens processadas com sucesso");
    public static readonly Counter<long> MessagesFailed =
        Meter.CreateCounter<long>("devjourney.messages.failed", description: "Mensagens enviadas para a DLQ");

    public static void Inject(Activity? activity, IDictionary<string, object?> headers) =>
        Propagator.Inject(
            new PropagationContext(activity?.Context ?? default, Baggage.Current),
            headers,
            (carrier, key, value) => carrier[key] = value);

    public static PropagationContext Extract(IDictionary<string, object?>? headers) =>
        Propagator.Extract(default, headers, (carrier, key) =>
            carrier is not null && carrier.TryGetValue(key, out var value) && value is byte[] bytes
                ? [Encoding.UTF8.GetString(bytes)]
                : []);
}
