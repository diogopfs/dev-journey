using DevJourney.Application.Common;

namespace DevJourney.Api.Infrastructure;

/// <summary>
/// Traduz um <see cref="Error"/> da camada Application para uma resposta HTTP no padrão
/// Problem Details (RFC 9457), o formato padronizado de erros em APIs REST.
/// </summary>
internal static class ResultExtensions
{
    public static IResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return error.Details is { Count: > 0 } details
            ? Results.ValidationProblem(details.ToDictionary(), detail: error.Message, title: error.Code, statusCode: status)
            : Results.Problem(detail: error.Message, title: error.Code, statusCode: status);
    }
}
