using DevJourney.Domain.Common;
using DevJourney.Domain.Users;
using DevJourney.UnitTests.Support;

namespace DevJourney.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Create_DeveCriarUsuarioAtivoComDadosInformados()
    {
        var user = TestData.User(name: "  João  ", role: UserRole.Admin);

        user.Id.ShouldNotBe(Guid.Empty);
        user.Name.ShouldBe("João");
        user.Role.ShouldBe(UserRole.Admin);
        user.IsActive.ShouldBeTrue();
        user.CreatedAt.ShouldBe(TestData.Now);
        user.UpdatedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Create_ComNomeInvalido_DeveLancarDomainException(string name)
    {
        Should.Throw<DomainException>(() => TestData.User(name: name));
    }

    [Fact]
    public void Create_ComNomeMuitoLongo_DeveLancarDomainException()
    {
        Should.Throw<DomainException>(() => TestData.User(name: new string('a', User.NameMaxLength + 1)));
    }

    [Fact]
    public void UpdateProfile_DeveAlterarDadosERegistrarDataDeAtualizacao()
    {
        var user = TestData.User();
        var later = TestData.Now.AddHours(1);

        user.UpdateProfile("Maria Souza", Email.Create("maria.souza@teste.com"), later);

        user.Name.ShouldBe("Maria Souza");
        user.Email.Value.ShouldBe("maria.souza@teste.com");
        user.UpdatedAt.ShouldBe(later);
    }

    [Fact]
    public void DeactivateEActivate_DevemAlternarStatus()
    {
        var user = TestData.User();

        user.Deactivate(TestData.Now);
        user.IsActive.ShouldBeFalse();

        user.Activate(TestData.Now);
        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void ChangePassword_ComHashVazio_DeveLancarDomainException()
    {
        var user = TestData.User();

        Should.Throw<DomainException>(() => user.ChangePassword(" ", TestData.Now));
    }
}
