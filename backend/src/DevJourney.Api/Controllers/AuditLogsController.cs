using DevJourney.Api.Security;
using DevJourney.Application.Abstractions.Audit;
using DevJourney.Application.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevJourney.Api.Controllers;

/// <summary>
/// Leitura dos logs de auditoria gravados no MongoDB pelo Worker (a partir dos eventos do RabbitMQ).
/// Exemplo de CQRS "light": a escrita acontece de forma assíncrona; a leitura vem de outro armazenamento.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public sealed class AuditLogsController(IAuditLogReader reader) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AuditLogEntry>>(StatusCodes.Status200OK)]
    public async Task<IResult> GetLatest([FromQuery] Guid? userId, [FromQuery] int limit = 50, CancellationToken ct = default) =>
        Results.Ok(await reader.GetLatestAsync(userId, limit, ct));
}
