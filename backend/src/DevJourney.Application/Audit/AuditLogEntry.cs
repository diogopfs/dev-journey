namespace DevJourney.Application.Audit;

/// <summary>
/// Registro de auditoria armazenado no banco NoSQL (MongoDB).
/// O payload é o JSON original do evento: documentos flexíveis são um ótimo caso de uso para NoSQL.
/// </summary>
public sealed record AuditLogEntry(
    Guid EventId,
    string EventType,
    Guid? UserId,
    Guid? PerformedBy,
    string Payload,
    DateTimeOffset OccurredAt,
    DateTimeOffset ProcessedAt);
