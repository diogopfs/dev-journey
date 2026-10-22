using DevJourney.Domain.Users;

namespace DevJourney.Application.Users;

// DTOs (Data Transfer Objects): contratos de entrada e saída. Nunca expomos a entidade diretamente,
// assim o PasswordHash jamais sai da API e o domínio pode evoluir sem quebrar o contrato.

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static UserResponse FromEntity(User user) => new(
        user.Id,
        user.Name,
        user.Email.Value,
        user.Role.ToString(),
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt);
}

public sealed record CreateUserRequest(string Name, string Email, string Password, UserRole Role = UserRole.User);

public sealed record UpdateUserRequest(string Name, string Email, UserRole? Role = null, bool? IsActive = null);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserQuery(string? Search = null, int Page = 1, int PageSize = 10);
