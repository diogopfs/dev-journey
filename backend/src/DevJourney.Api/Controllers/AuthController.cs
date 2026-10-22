using DevJourney.Api.Infrastructure;
using DevJourney.Api.Security;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Application.Auth;
using DevJourney.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevJourney.Api.Controllers;

/// <summary>
/// Endpoints de autenticação. Controllers são "finos": recebem HTTP, delegam ao caso de uso e traduzem o resultado.
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthPolicies.AuthRateLimit)]
public sealed class AuthController(IAuthService authService, IUserService userService, AuthMetrics metrics) : ControllerBase
{
    public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return result.IsSuccess
            ? Results.Created($"/api/users/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        metrics.RecordLogin(result.IsSuccess);
        return result.IsSuccess ? IssueTokens(result.Value) : result.Error!.ToProblem();
    }

    /// <summary>Troca o refresh token (cookie) por um novo par de tokens (rotação).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> Refresh(CancellationToken ct)
    {
        var result = await authService.RefreshAsync(RefreshTokenCookie.Read(Request) ?? string.Empty, ct);
        if (result.IsSuccess) return IssueTokens(result.Value);

        RefreshTokenCookie.Delete(Response);
        return result.Error!.ToProblem();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> Logout(CancellationToken ct)
    {
        await authService.LogoutAsync(RefreshTokenCookie.Read(Request) ?? string.Empty, ct);
        RefreshTokenCookie.Delete(Response);
        return Results.NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<IResult> Me([FromServices] ICurrentUser currentUser, CancellationToken ct)
    {
        var result = await userService.GetByIdAsync(currentUser.Id!.Value, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    private IResult IssueTokens(AuthResult auth)
    {
        RefreshTokenCookie.Append(Response, auth.RefreshToken, auth.RefreshTokenExpiresAt);
        return Results.Ok(new TokenResponse(auth.AccessToken, auth.AccessTokenExpiresAt, auth.User));
    }
}
