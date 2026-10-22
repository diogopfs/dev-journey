using DevJourney.Domain.Common;
using DevJourney.Domain.Users;

namespace DevJourney.UnitTests.Domain;

public class EmailTests
{
    [Fact]
    public void Create_DeveNormalizarParaMinusculoESemEspacos()
    {
        var email = Email.Create("  Maria.Silva@Teste.COM ");

        email.Value.ShouldBe("maria.silva@teste.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba")]
    [InlineData("a@b")]
    [InlineData("com espaco@teste.com")]
    public void Create_ComValorInvalido_DeveLancarDomainException(string? value)
    {
        Should.Throw<DomainException>(() => Email.Create(value));
    }

    [Fact]
    public void Emails_ComMesmoValor_DevemSerIguais()
    {
        // Value Objects são comparados pelo valor, não pela referência.
        Email.Create("a@b.com").ShouldBe(Email.Create("A@B.com"));
    }
}
