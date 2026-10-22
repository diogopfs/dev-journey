using DevJourney.Application.Audit;

namespace DevJourney.Application.Abstractions.Audit;

/// <summary>
/// Interfaces separadas para leitura e escrita (Interface Segregation Principle):
/// o Worker só escreve, a API só lê.
/// </summary>
public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken ct = default);
}

public interface IAuditLogReader
{
    Task<IReadOnlyList<AuditLogEntry>> GetLatestAsync(Guid? userId, int limit, CancellationToken ct = default);
}
