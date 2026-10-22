using DevJourney.Application.Abstractions.Messaging;
using DevJourney.Application.Abstractions.Persistence;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Application.Common;
using DevJourney.Application.Events;
using DevJourney.Domain.Users;
using FluentValidation;

namespace DevJourney.Application.Users;

/// <summary>
/// Casos de uso do CRUD de usuários.
/// Depende apenas de abstrações (repositório, hasher, publisher...), por isso é fácil de testar com mocks.
/// </summary>
public sealed class UserService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IEventPublisher eventPublisher,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator) : IUserService
{
    private const int MaxPageSize = 100;

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct);
        return user is null ? UserErrors.NotFound(id) : UserResponse.FromEntity(user);
    }

    public async Task<PagedResult<UserResponse>> ListAsync(UserQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var (items, total) = await users.ListAsync(query.Search, page, pageSize, ct);
        return new PagedResult<UserResponse>(items.Select(UserResponse.FromEntity).ToList(), page, pageSize, total);
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return validation.ToError();

        var email = Email.Create(request.Email);
        if (await users.EmailExistsAsync(email, ct: ct)) return UserErrors.EmailInUse(email);

        var user = User.Create(
            request.Name,
            email,
            passwordHasher.Hash(request.Password),
            request.Role,
            timeProvider.GetUtcNow());

        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(
            new UserCreatedEvent(user.Id, user.Name, user.Email.Value, user.Role.ToString(), currentUser.Id), ct);

        return UserResponse.FromEntity(user);
    }

    public async Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        // Regra de autorização de negócio: usuário comum só altera o próprio cadastro e não muda perfil/status.
        if (!currentUser.IsAdmin)
        {
            if (currentUser.Id != id) return UserErrors.CannotEditOthers;
            if (request.Role.HasValue || request.IsActive.HasValue) return UserErrors.CannotChangeRoleOrStatus;
        }

        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return validation.ToError();

        var user = await users.GetByIdAsync(id, ct);
        if (user is null) return UserErrors.NotFound(id);

        var email = Email.Create(request.Email);
        if (await users.EmailExistsAsync(email, ignoreUserId: id, ct)) return UserErrors.EmailInUse(email);

        var now = timeProvider.GetUtcNow();
        user.UpdateProfile(request.Name, email, now);

        if (request.Role is { } role && role != user.Role)
            user.ChangeRole(role, now);

        if (request.IsActive is { } isActive && isActive != user.IsActive)
        {
            if (isActive) user.Activate(now);
            else user.Deactivate(now);
        }

        await unitOfWork.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(
            new UserUpdatedEvent(user.Id, user.Name, user.Email.Value, user.Role.ToString(), user.IsActive, currentUser.Id), ct);

        return UserResponse.FromEntity(user);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.Id == id) return UserErrors.CannotDeleteSelf;

        var user = await users.GetByIdAsync(id, ct);
        if (user is null) return UserErrors.NotFound(id);

        users.Remove(user);
        await unitOfWork.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(new UserDeletedEvent(user.Id, user.Email.Value, currentUser.Id), ct);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id != id) return UserErrors.CannotEditOthers;

        var validation = await changePasswordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return validation.ToError();

        var user = await users.GetByIdAsync(id, ct);
        if (user is null) return UserErrors.NotFound(id);

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            return Error.Validation("Senha atual incorreta.");

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
