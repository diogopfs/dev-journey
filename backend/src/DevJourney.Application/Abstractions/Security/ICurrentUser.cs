namespace DevJourney.Application.Abstractions.Security;

/// <summary>
/// Quem está executando a operação. Implementado na API a partir das claims do JWT.
/// </summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
