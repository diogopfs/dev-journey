using DevJourney.Domain.Common;

namespace DevJourney.Domain.Users;

/// <summary>
/// Entidade raiz (Aggregate Root) de usuário.
/// Os setters são privados: o estado só muda através de métodos que validam as regras de negócio
/// (encapsulamento / "modelo rico" em vez de "modelo anêmico").
/// </summary>
public sealed class User : Entity
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 120;

    public string Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    // Construtor exigido pelo EF Core
    private User() { }

    public static User Create(string name, Email email, string passwordHash, UserRole role, DateTimeOffset now)
    {
        var user = new User
        {
            Email = email,
            Role = role,
            IsActive = true,
            CreatedAt = now
        };
        user.SetName(name);
        user.SetPasswordHash(passwordHash);
        return user;
    }

    public void UpdateProfile(string name, Email email, DateTimeOffset now)
    {
        SetName(name);
        Email = email;
        Touch(now);
    }

    public void ChangeRole(UserRole role, DateTimeOffset now)
    {
        Role = role;
        Touch(now);
    }

    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        SetPasswordHash(passwordHash);
        Touch(now);
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        Touch(now);
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        Touch(now);
    }

    private void SetName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is < NameMinLength or > NameMaxLength)
            throw new DomainException($"Nome deve ter entre {NameMinLength} e {NameMaxLength} caracteres.");
        Name = trimmed;
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Hash de senha é obrigatório.");
        PasswordHash = passwordHash;
    }
}
