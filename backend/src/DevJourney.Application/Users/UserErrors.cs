using DevJourney.Application.Common;
using DevJourney.Domain.Users;

namespace DevJourney.Application.Users;

/// <summary>
/// Catálogo centralizado de erros do módulo de usuários.
/// </summary>
public static class UserErrors
{
    public static Error NotFound(Guid id) => Error.NotFound($"Usuário '{id}' não encontrado.");
    public static Error EmailInUse(Email email) => Error.Conflict($"O e-mail '{email}' já está em uso.");

    public static readonly Error CannotEditOthers = Error.Forbidden("Você só pode alterar o seu próprio cadastro.");
    public static readonly Error CannotChangeRoleOrStatus = Error.Forbidden("Apenas administradores podem alterar perfil ou status.");
    public static readonly Error CannotDeleteSelf = Error.Validation("Você não pode excluir o seu próprio usuário.");
}
