using DevJourney.Domain.Users;

namespace DevJourney.UnitTests.Support;

/// <summary>
/// Object Mother: centraliza a criação de objetos de teste para manter os testes curtos e legíveis.
/// </summary>
internal static class TestData
{
    public const string ValidPassword = "Senha@123";
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public static User User(string name = "Maria Silva", string email = "maria@teste.com", UserRole role = UserRole.User) =>
        DevJourney.Domain.Users.User.Create(name, Email.Create(email), "hash-da-senha", role, Now);
}
