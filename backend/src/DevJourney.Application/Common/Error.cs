namespace DevJourney.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}

/// <summary>
/// Descreve uma falha esperada (regra de negócio, validação, recurso inexistente...).
/// A camada de API traduz o <see cref="ErrorType"/> para o status HTTP adequado.
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error Validation(string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new("validation_error", message, ErrorType.Validation, details);

    public static Error NotFound(string message) => new("not_found", message, ErrorType.NotFound);
    public static Error Conflict(string message) => new("conflict", message, ErrorType.Conflict);
    public static Error Unauthorized(string message) => new("unauthorized", message, ErrorType.Unauthorized);
    public static Error Forbidden(string message) => new("forbidden", message, ErrorType.Forbidden);
}
