using DevJourney.Application.Abstractions.Messaging;
using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Application.Auth;
using DevJourney.Application.Common;
using DevJourney.Application.Events;
using DevJourney.Domain.Auth;
using DevJourney.Domain.Users;
using DevJourney.UnitTests.Support;
using Microsoft.Extensions.Time.Testing;

namespace DevJourney.UnitTests.Application;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly FakeTimeProvider _time = new(TestData.Now);
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokens.CreateAccessToken(Arg.Any<User>()).Returns(new AccessToken("jwt", TestData.Now.AddMinutes(15)));
        _tokens.CreateRefreshToken().Returns(new GeneratedRefreshToken("novo-refresh", "novo-hash", TestData.Now.AddDays(7)));
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns(ci => $"hash:{ci.Arg<string>()}");

        _sut = new AuthService(
            _users, _refreshTokens, _unitOfWork, _hasher, _tokens, _publisher, _time,
            new RegisterRequestValidator(), new LoginRequestValidator());
    }

    [Fact]
    public async Task RegisterAsync_SempreCriaUsuarioComPerfilUser()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        var result = await _sut.RegisterAsync(new RegisterRequest("Novo Usuário", "novo@teste.com", TestData.ValidPassword));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Role.ShouldBe(nameof(UserRole.User));
        await _publisher.Received(1).PublishAsync(Arg.Any<UserCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_ComCredenciaisValidas_DeveEmitirTokensEPublicarEvento()
    {
        var user = TestData.User();
        _users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(user.PasswordHash, TestData.ValidPassword).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest(user.Email.Value, TestData.ValidPassword));

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("jwt");
        result.Value.RefreshToken.ShouldBe("novo-refresh");
        _refreshTokens.Received(1).Add(Arg.Is<RefreshToken>(t => t.TokenHash == "novo-hash" && t.UserId == user.Id));
        await _publisher.Received(1).PublishAsync(Arg.Any<UserLoggedInEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_ComSenhaErrada_DeveRetornarUnauthorized()
    {
        var user = TestData.User();
        _users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _sut.LoginAsync(new LoginRequest(user.Email.Value, "Errada@1"));

        result.Error!.Type.ShouldBe(ErrorType.Unauthorized);
        _tokens.DidNotReceive().CreateAccessToken(Arg.Any<User>());
    }

    [Fact]
    public async Task LoginAsync_ComUsuarioInativo_DeveRetornarUnauthorized()
    {
        var user = TestData.User();
        user.Deactivate(TestData.Now);
        _users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest(user.Email.Value, TestData.ValidPassword));

        result.Error!.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RefreshAsync_ComTokenValido_DeveRotacionarToken()
    {
        var user = TestData.User();
        var stored = RefreshToken.Issue(user.Id, "hash:antigo", TestData.Now.AddDays(7), TestData.Now);
        _refreshTokens.GetByHashAsync("hash:antigo", Arg.Any<CancellationToken>()).Returns(stored);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.RefreshAsync("antigo");

        result.IsSuccess.ShouldBeTrue();
        stored.IsRevoked.ShouldBeTrue();
        stored.ReplacedByTokenHash.ShouldBe("novo-hash");
        _refreshTokens.Received(1).Add(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task RefreshAsync_ComTokenJaRevogado_DeveRevogarTodasAsSessoes()
    {
        var userId = Guid.NewGuid();
        var reused = RefreshToken.Issue(userId, "hash:roubado", TestData.Now.AddDays(7), TestData.Now);
        reused.Revoke(TestData.Now);
        var otherSession = RefreshToken.Issue(userId, "hash:outro", TestData.Now.AddDays(7), TestData.Now);

        _refreshTokens.GetByHashAsync("hash:roubado", Arg.Any<CancellationToken>()).Returns(reused);
        _refreshTokens.ListActiveByUserAsync(userId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([otherSession]);

        var result = await _sut.RefreshAsync("roubado");

        result.Error!.Type.ShouldBe(ErrorType.Unauthorized);
        otherSession.IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshAsync_ComTokenExpirado_DeveRetornarUnauthorized()
    {
        var stored = RefreshToken.Issue(Guid.NewGuid(), "hash:velho", TestData.Now.AddDays(1), TestData.Now);
        _refreshTokens.GetByHashAsync("hash:velho", Arg.Any<CancellationToken>()).Returns(stored);
        _time.Advance(TimeSpan.FromDays(2));

        var result = await _sut.RefreshAsync("velho");

        result.Error!.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task LogoutAsync_DeveRevogarToken()
    {
        var stored = RefreshToken.Issue(Guid.NewGuid(), "hash:sessao", TestData.Now.AddDays(7), TestData.Now);
        _refreshTokens.GetByHashAsync("hash:sessao", Arg.Any<CancellationToken>()).Returns(stored);

        await _sut.LogoutAsync("sessao");

        stored.IsRevoked.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
