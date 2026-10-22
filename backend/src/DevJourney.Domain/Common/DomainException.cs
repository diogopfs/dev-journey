namespace DevJourney.Domain.Common;

/// <summary>
/// Representa a violação de uma regra de negócio (invariante) do domínio.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
