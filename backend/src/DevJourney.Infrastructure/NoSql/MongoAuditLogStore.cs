using System.Diagnostics;
using DevJourney.Application.Abstractions.Audit;
using DevJourney.Application.Audit;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace DevJourney.Infrastructure.NoSql;

internal sealed class MongoAuditLogStore : IAuditLogWriter, IAuditLogReader
{
    public const string ActivitySourceName = "DevJourney.MongoDB";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private readonly IMongoCollection<AuditLogDocument> _collection;

    public MongoAuditLogStore(IMongoDatabase database, IOptions<MongoOptions> options)
    {
        _collection = database.GetCollection<AuditLogDocument>(options.Value.AuditCollection);
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        using var activity = StartActivity("audit_logs upsert");

        var document = new AuditLogDocument
        {
            Id = entry.EventId,
            EventType = entry.EventType,
            UserId = entry.UserId,
            PerformedBy = entry.PerformedBy,
            Payload = BsonDocument.Parse(entry.Payload),
            OccurredAt = entry.OccurredAt.UtcDateTime,
            ProcessedAt = entry.ProcessedAt.UtcDateTime
        };

        await _collection.ReplaceOneAsync(d => d.Id == document.Id, document, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetLatestAsync(Guid? userId, int limit, CancellationToken ct = default)
    {
        using var activity = StartActivity("audit_logs find");

        var filter = userId is null
            ? Builders<AuditLogDocument>.Filter.Empty
            : Builders<AuditLogDocument>.Filter.Eq(d => d.UserId, userId);

        var documents = await _collection.Find(filter)
            .SortByDescending(d => d.OccurredAt)
            .Limit(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);

        return documents.Select(d => new AuditLogEntry(
                d.Id,
                d.EventType,
                d.UserId,
                d.PerformedBy,
                d.Payload.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }),
                new DateTimeOffset(d.OccurredAt, TimeSpan.Zero),
                new DateTimeOffset(d.ProcessedAt, TimeSpan.Zero)))
            .ToList();
    }

    /// <summary>Cria os índices da coleção (operação idempotente).</summary>
    public Task EnsureIndexesAsync(CancellationToken ct = default) =>
        _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(d => d.UserId).Descending(d => d.OccurredAt)),
            cancellationToken: ct);

    private Activity? StartActivity(string name)
    {
        var activity = ActivitySource.StartActivity(name, ActivityKind.Client);
        activity?.SetTag("db.system", "mongodb");
        activity?.SetTag("db.collection.name", _collection.CollectionNamespace.CollectionName);
        return activity;
    }
}
