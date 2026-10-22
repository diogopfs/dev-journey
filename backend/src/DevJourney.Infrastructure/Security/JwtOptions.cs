using System.ComponentModel.DataAnnotations;

namespace DevJourney.Infrastructure.Security;

/// <summary>
/// Configurações do JWT (Options Pattern). Validadas no startup: a aplicação nem sobe com configuração inválida.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = null!;
    [Required] public string Audience { get; init; } = null!;

    /// <summary>Chave HMAC-SHA256. Mínimo de 32 caracteres (256 bits). Em produção use um secret manager!</summary>
    [Required, MinLength(32)] public string SigningKey { get; init; } = null!;

    [Range(1, 60)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(1, 90)] public int RefreshTokenDays { get; init; } = 7;
}
