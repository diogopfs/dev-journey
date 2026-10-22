using DevJourney.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevJourney.Api.Infrastructure;

/// <summary>
/// Último nível de defesa: transforma exceções não tratadas em Problem Details,
/// sem vazar stack trace para o cliente (detalhes ficam apenas no log/trace).
/// </summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = exception switch
        {
            DomainException ex => (StatusCodes.Status400BadRequest, "domain_error", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "server_error", "Ocorreu um erro inesperado.")
        };

        if (status >= 500) logger.LogError(exception, "Erro não tratado");

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}
