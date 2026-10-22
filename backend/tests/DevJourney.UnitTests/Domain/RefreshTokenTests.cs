using DevJourney.Domain.Auth;
using DevJourney.UnitTests.Support;

namespace DevJourney.UnitTests.Domain;

public class RefreshTokenTests
{
    private static RefreshToken NewToken() =>
        RefreshToken.Issue(Guid.NewGuid(), "hash", expiresAt: TestData.Now.AddDays(7), now: TestData.Now);

    [Fact]
    public void IsActive_AntesDeExpirarESemRevogacao_DeveSerVerdadeiro()
    {
        NewToken().IsActive(TestData.Now.AddDays(1)).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_DepoisDeExpirar_DeveSerFalso()
    {
        NewToken().IsActive(TestData.Now.AddDays(8)).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_DeveMarcarComoRevogadoEGuardarSubstituto()
    {
        var token = NewToken();

        token.Revoke(TestData.Now, replacedByTokenHash: "novo-hash");

        token.IsRevoked.ShouldBeTrue();
        token.ReplacedByTokenHash.ShouldBe("novo-hash");
        token.IsActive(TestData.Now).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_Duplicado_NaoDeveSobrescreverPrimeiraRevogacao()
    {
        var token = NewToken();
        token.Revoke(TestData.Now, "primeiro");

        token.Revoke(TestData.Now.AddMinutes(5), "segundo");

        token.RevokedAt.ShouldBe(TestData.Now);
        token.ReplacedByTokenHash.ShouldBe("primeiro");
    }
}
