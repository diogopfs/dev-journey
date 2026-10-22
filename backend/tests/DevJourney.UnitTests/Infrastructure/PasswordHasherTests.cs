using DevJourney.Infrastructure.Security;

namespace DevJourney.UnitTests.Infrastructure;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_DeveGerarValoresDiferentesParaAMesmaSenha_PorCausaDoSalt()
    {
        _sut.Hash("Senha@123").ShouldNotBe(_sut.Hash("Senha@123"));
    }

    [Fact]
    public void Verify_DeveValidarSomenteASenhaCorreta()
    {
        var hash = _sut.Hash("Senha@123");

        _sut.Verify(hash, "Senha@123").ShouldBeTrue();
        _sut.Verify(hash, "senha@123").ShouldBeFalse();
    }
}
