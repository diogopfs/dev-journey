using DevJourney.Application.Abstractions.Messaging;
using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Application.Common;
using DevJourney.Application.Events;
using DevJourney.Application.Users;
using DevJourney.Domain.Auth;
using DevJourney.Domain.Users;
using FluentValidation;

namespace DevJourney.Application.Auth;

/// <summary>
/// Fluxo de autenticação baseado em tokens:
/// - Access token (JWT) de curta duração, enviado no header Authorization: Bearer.
/// - Refresh token opaco de longa duração, com rotação a cada uso e detecção de reutilização.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    // Mensagem genérica de propósito: não revelamos se o e-mail existe (evita "user enumeration").
    private static readonly Error InvalidCredentials = Error.Unauthorized("E-mail ou senha inválidos.");
    private static readonly Error InvalidRefreshToken = Error.Unauthorized("Sessão inválida ou expirada.");

    public async Task<Result<UserResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var validation = await registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return validation.ToError();

        var email = Email.Create(request.Email);
        if (await users.EmailExistsAsync(email, ct: ct)) return UserErrors.EmailInUse(email);

        // Auto-cadastro sempre cria perfil "User". Apenas um Admin pode promover outros usuários.
        var user = User.Create(request.Name, email, passwordHasher.Hash(request.Password), UserRole.User, timeProvider.GetUtcNow());

        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(
            new UserCreatedEvent(user.Id, user.Name, user.Email.Value, user.Role.ToString(), PerformedBy: user.Id), ct);

        return UserResponse.FromEntity(user);
    }

    public async Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validation = await loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return validation.ToError();

        Email email;
        try { email = Email.Create(request.Email); }
        catch (Domain.Common.DomainException) { return InvalidCredentials; }

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || !user.IsActive || !passwordHasher.Verify(user.PasswordHash, request.Password))
            return InvalidCredentials;

        var result = await IssueTokensAsync(user, ct);

        await eventPublisher.PublishAsync(new UserLoggedInEvent(user.Id, user.Email.Value), ct);

        return result;
    }

    public async Task<Result<AuthResult>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return InvalidRefreshToken;

        var now = timeProvider.GetUtcNow();
        var stored = await refreshTokens.GetByHashAsync(tokenService.HashRefreshToken(refreshToken), ct);
        if (stored is null) return InvalidRefreshToken;

        if (stored.IsRevoked)
        {
            // Detecção de reutilização: um token já rotacionado foi apresentado novamente.
            // Pode indicar roubo de token, então revogamos todas as sessões do usuário.
            await RevokeAllAsync(stored.UserId, now, ct);
            return InvalidRefreshToken;
        }

        if (!stored.IsActive(now)) return InvalidRefreshToken;

        var user = await users.GetByIdAsync(stored.UserId, ct);
        if (user is null || !user.IsActive) return InvalidRefreshToken;

        return await IssueTokensAsync(user, ct, tokenToRotate: stored);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var stored = await refreshTokens.GetByHashAsync(tokenService.HashRefreshToken(refreshToken), ct);
        if (stored is null) return;

        stored.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct, RefreshToken? tokenToRotate = null)
    {
        var now = timeProvider.GetUtcNow();
        var accessToken = tokenService.CreateAccessToken(user);
        var newRefresh = tokenService.CreateRefreshToken();

        tokenToRotate?.Revoke(now, replacedByTokenHash: newRefresh.TokenHash);
        refreshTokens.Add(RefreshToken.Issue(user.Id, newRefresh.TokenHash, newRefresh.ExpiresAt, now));
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthResult(
            accessToken.Token,
            accessToken.ExpiresAt,
            newRefresh.Token,
            newRefresh.ExpiresAt,
            UserResponse.FromEntity(user));
    }

    private async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var token in await refreshTokens.ListActiveByUserAsync(userId, now, ct))
            token.Revoke(now);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
