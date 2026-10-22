using DevJourney.Domain.Users;
using DevJourney.Infrastructure.Security;
using DevJourney.UnitTests.Support;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DevJourney.UnitTests.Infrastructure;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut = new(
        Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = new string('k', 64),
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }),
        new FakeTimeProvider(DateTimeOffset.UtcNow));

    [Fact]
    public void CreateAccessToken_DeveConterClaimsDoUsuario()
    {
        var user = TestData.User(role: UserRole.Admin);

        var token = _sut.CreateAccessToken(user);
        var jwt = new JsonWebToken(token.Token);

        jwt.Issuer.ShouldBe("test-issuer");
        jwt.Audiences.ShouldContain("test-audience");
        jwt.Subject.ShouldBe(user.Id.ToString());
        jwt.GetClaim("email").Value.ShouldBe(user.Email.Value);
        jwt.GetClaim("role").Value.ShouldBe("Admin");
        jwt.Alg.ShouldBe("HS256");
    }

    [Fact]
    public void CreateRefreshToken_DeveGerarTokensUnicosComHashCorrespondente()
    {
        var first = _sut.CreateRefreshToken();
        var second = _sut.CreateRefreshToken();

        first.Token.ShouldNotBe(second.Token);
        first.TokenHash.ShouldBe(_sut.HashRefreshToken(first.Token));
        first.TokenHash.ShouldNotBe(first.Token); // nunca persistimos o valor original
    }
}
