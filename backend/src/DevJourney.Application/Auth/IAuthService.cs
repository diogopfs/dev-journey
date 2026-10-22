using DevJourney.Application.Common;
using DevJourney.Application.Users;

namespace DevJourney.Application.Auth;

public interface IAuthService
{
    Task<Result<UserResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResult>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
