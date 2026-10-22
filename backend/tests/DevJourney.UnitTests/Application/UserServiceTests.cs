using DevJourney.Application.Abstractions.Messaging;
using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Application.Common;
using DevJourney.Application.Events;
using DevJourney.Application.Users;
using DevJourney.Domain.Users;
using DevJourney.UnitTests.Support;
using Microsoft.Extensions.Time.Testing;

namespace DevJourney.UnitTests.Application;

/// <summary>
/// Testes do caso de uso com "test doubles" (NSubstitute): isolamos a regra de negócio de banco, broker etc.
/// Padrão AAA: Arrange (prepara), Act (executa), Assert (verifica).
/// </summary>
public class UserServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _currentUser.IsAdmin.Returns(true);
        _currentUser.Id.Returns(Guid.NewGuid());

        _sut = new UserService(
            _users, _unitOfWork, _hasher, _publisher, _currentUser,
            new FakeTimeProvider(TestData.Now),
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator(),
            new ChangePasswordRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_ComDadosValidos_DeveSalvarEPublicarEvento()
    {
        var request = new CreateUserRequest("Maria Silva", "maria@teste.com", TestData.ValidPassword, UserRole.User);

        var result = await _sut.CreateAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("maria@teste.com");
        _users.Received(1).Add(Arg.Is<User>(u => u.PasswordHash == "hashed"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<UserCreatedEvent>(e => e.Email == "maria@teste.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ComEmailDuplicado_DeveRetornarConflito()
    {
        _users.EmailExistsAsync(Arg.Any<Email>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateUserRequest("Maria", "maria@teste.com", TestData.ValidPassword));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        _users.DidNotReceive().Add(Arg.Any<User>());
        await _publisher.DidNotReceiveWithAnyArgs().PublishAsync<IntegrationEvent>(default!, default);
    }

    [Theory]
    [InlineData("fraca")]
    [InlineData("semnumero@A")]
    [InlineData("SEMMINUSCULA1!")]
    [InlineData("SemEspecial123")]
    public async Task CreateAsync_ComSenhaFraca_DeveRetornarErroDeValidacao(string password)
    {
        var result = await _sut.CreateAsync(new CreateUserRequest("Maria", "maria@teste.com", password));

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Details!.ShouldContainKey(nameof(CreateUserRequest.Password));
    }

    [Fact]
    public async Task GetByIdAsync_QuandoNaoExiste_DeveRetornarNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_UsuarioComumAlterandoOutro_DeveRetornarForbidden()
    {
        _currentUser.IsAdmin.Returns(false);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateUserRequest("Outro Nome", "outro@teste.com"));

        result.Error.ShouldBe(UserErrors.CannotEditOthers);
    }

    [Fact]
    public async Task UpdateAsync_UsuarioComumAlterandoProprioPerfil_NaoPodeMudarRole()
    {
        var me = TestData.User();
        _currentUser.IsAdmin.Returns(false);
        _currentUser.Id.Returns(me.Id);

        var result = await _sut.UpdateAsync(me.Id, new UpdateUserRequest("Maria", "maria@teste.com", Role: UserRole.Admin));

        result.Error.ShouldBe(UserErrors.CannotChangeRoleOrStatus);
    }

    [Fact]
    public async Task UpdateAsync_AdminDesativandoUsuario_DeveAtualizarEPublicarEvento()
    {
        var user = TestData.User();
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.UpdateAsync(user.Id,
            new UpdateUserRequest("Maria Atualizada", "maria@teste.com", UserRole.Admin, IsActive: false));

        result.IsSuccess.ShouldBeTrue();
        user.Name.ShouldBe("Maria Atualizada");
        user.Role.ShouldBe(UserRole.Admin);
        user.IsActive.ShouldBeFalse();
        await _publisher.Received(1).PublishAsync(Arg.Any<UserUpdatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ProprioUsuario_DeveSerBloqueado()
    {
        var myId = Guid.NewGuid();
        _currentUser.Id.Returns(myId);

        var result = await _sut.DeleteAsync(myId);

        result.Error.ShouldBe(UserErrors.CannotDeleteSelf);
    }

    [Fact]
    public async Task DeleteAsync_UsuarioExistente_DeveRemoverEPublicarEvento()
    {
        var user = TestData.User();
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.DeleteAsync(user.Id);

        result.IsSuccess.ShouldBeTrue();
        _users.Received(1).Remove(user);
        await _publisher.Received(1).PublishAsync(
            Arg.Is<UserDeletedEvent>(e => e.UserId == user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_DeveLimitarTamanhoDaPagina()
    {
        _users.ListAsync(null, 1, 100, Arg.Any<CancellationToken>()).Returns(([], 0));

        var result = await _sut.ListAsync(new UserQuery(Page: 0, PageSize: 1000));

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(100);
    }

    [Fact]
    public async Task ChangePasswordAsync_ComSenhaAtualErrada_DeveFalhar()
    {
        var user = TestData.User();
        _currentUser.Id.Returns(user.Id);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(user.PasswordHash, "Errada@123").Returns(false);

        var result = await _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("Errada@123", "NovaSenha@456"));

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
