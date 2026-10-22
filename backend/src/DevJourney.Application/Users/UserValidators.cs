using DevJourney.Domain.Users;
using FluentValidation;

namespace DevJourney.Application.Users;

/// <summary>
/// Regras de senha reaproveitadas por todos os validadores (DRY).
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 8;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(MinLength).WithMessage($"Senha deve ter no mínimo {MinLength} caracteres.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("Senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("Senha deve conter ao menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Senha deve conter ao menos um caractere especial.");
}

internal static class UserRules
{
    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Nome é obrigatório.")
            .Length(User.NameMinLength, User.NameMaxLength)
            .WithMessage($"Nome deve ter entre {User.NameMinLength} e {User.NameMaxLength} caracteres.");

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(Email.MaxLength)
            .EmailAddress().WithMessage("E-mail inválido.");
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Role).IsInEnum().When(x => x.Role.HasValue);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("A nova senha deve ser diferente da atual.");
    }
}
