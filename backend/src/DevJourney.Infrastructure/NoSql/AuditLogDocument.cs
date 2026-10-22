using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DevJourney.Infrastructure.NoSql;

/// <summary>
/// Formato do documento no MongoDB. O _id é o EventId: se a mesma mensagem for entregue duas vezes
/// (o RabbitMQ garante entrega "at-least-once"), o upsert torna o processamento IDEMPOTENTE.
/// </summary>
internal sealed class AuditLogDocument
{
    [BsonId, BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public string EventType { get; set; } = null!;

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? UserId { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? PerformedBy { get; set; }

    /// <summary>Payload guardado como documento BSON (consultável), não como string.</summary>
    public BsonDocument Payload { get; set; } = null!;

    public DateTime OccurredAt { get; set; }
    public DateTime ProcessedAt { get; set; }
}
