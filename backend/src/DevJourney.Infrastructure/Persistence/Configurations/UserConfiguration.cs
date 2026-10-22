using DevJourney.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevJourney.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento via Fluent API: mantém o domínio livre de atributos de persistência.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(User.NameMaxLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        // Value Object "Email" mapeado como owned type: vira a coluna "Email" na própria tabela users.
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(Email.MaxLength).IsRequired();
            email.HasIndex(e => e.Value).IsUnique();
        });
        builder.Navigation(u => u.Email).IsRequired();
    }
}
