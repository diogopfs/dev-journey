using DevJourney.Application.Common;

namespace DevJourney.Application.Users;

public interface IUserService
{
    Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<UserResponse>> ListAsync(UserQuery query, CancellationToken ct = default);
    Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken ct = default);
}
