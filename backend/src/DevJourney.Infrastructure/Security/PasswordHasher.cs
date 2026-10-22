using DevJourney.Domain.Users;
using Microsoft.AspNetCore.Identity;
using IAppPasswordHasher = DevJourney.Application.Abstractions.Security.IPasswordHasher;

namespace DevJourney.Infrastructure.Security;

/// <summary>
/// Adapter sobre o PasswordHasher do ASP.NET Core Identity (PBKDF2 + HMAC-SHA512, 100 mil iterações, salt aleatório).
/// Nunca guarde senhas em texto puro nem com hashes rápidos como MD5/SHA1.
/// </summary>
internal sealed class PasswordHasher : IAppPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hashedPassword, string providedPassword) =>
        _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword) != PasswordVerificationResult.Failed;
}
