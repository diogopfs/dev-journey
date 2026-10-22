using DevJourney.Application.Users;
using FluentValidation;

namespace DevJourney.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).ValidName();
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).StrongPassword();
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
