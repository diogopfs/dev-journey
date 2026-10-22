using System.Text.Json;
using DevJourney.Application.Abstractions.Audit;
using DevJourney.Application.Audit;
using DevJourney.Infrastructure.Messaging;
using DevJourney.Worker.Messaging;

namespace DevJourney.Worker.Consumers;

/// <summary>
/// Recebe TODOS os eventos de usuário (binding "user.*") e grava um log de auditoria no MongoDB.
/// </summary>
public sealed class AuditLogConsumer(
    RabbitMqConnection connection,
    IAuditLogWriter writer,
    TimeProvider timeProvider,
    ILogger<AuditLogConsumer> logger) : RabbitMqConsumer(connection, logger)
{
    protected override string QueueName => "devjourney.audit-log";
    protected override string[] RoutingKeys => ["user.*"];

    protected override async Task HandleAsync(ConsumedMessage message, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(message.Body);
        var root = json.RootElement;

        var entry = new AuditLogEntry(
            EventId: root.GetProperty("eventId").GetGuid(),
            EventType: message.RoutingKey,
            UserId: TryGetGuid(root, "userId"),
            PerformedBy: TryGetGuid(root, "performedBy"),
            Payload: message.Body,
            OccurredAt: root.GetProperty("occurredAt").GetDateTimeOffset(),
            ProcessedAt: timeProvider.GetUtcNow());

        await writer.WriteAsync(entry, ct);
        logger.LogInformation("Auditoria registrada: {EventType} para usuário {UserId}", entry.EventType, entry.UserId);
    }

    private static Guid? TryGetGuid(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && value.TryGetGuid(out var guid)
            ? guid
            : null;
}
