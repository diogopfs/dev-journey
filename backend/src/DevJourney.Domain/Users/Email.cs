using System.Text.RegularExpressions;
using DevJourney.Domain.Common;

namespace DevJourney.Domain.Users;

/// <summary>
/// Value Object: não tem identidade própria, é definido pelo seu valor e é imutável.
/// Garante que nenhum e-mail inválido exista dentro do domínio.
/// </summary>
public sealed partial record Email
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("E-mail é obrigatório.");

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength || !EmailRegex().IsMatch(normalized))
            throw new DomainException($"E-mail '{value}' é inválido.");

        return new Email(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
