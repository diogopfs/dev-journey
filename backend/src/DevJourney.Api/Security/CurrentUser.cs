using System.Security.Claims;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DevJourney.Api.Security;

/// <summary>
/// Adapter que expõe para a camada Application quem é o usuário autenticado, lendo as claims do JWT.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? Id => Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => Principal?.IsInRole(nameof(UserRole.Admin)) ?? false;
}
