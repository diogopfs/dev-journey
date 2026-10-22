using DevJourney.Api.Infrastructure;
using DevJourney.Api.Security;
using DevJourney.Application.Abstractions.Security;
using DevJourney.Application.Common;
using DevJourney.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevJourney.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] UserQuery query, CancellationToken ct) =>
        Results.Ok(await userService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await userService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var result = await userService.CreateAsync(request, ct);
        return result.IsSuccess
            ? Results.Created($"/api/users/{result.Value.Id}", result.Value)
            : result.Error!.ToProblem();
    }

    /// <summary>Admin altera qualquer usuário; usuário comum altera apenas nome/e-mail do próprio cadastro.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IResult> Update(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var result = await userService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await userService.DeleteAsync(id, ct);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }

    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ChangePassword(ChangePasswordRequest request, [FromServices] ICurrentUser currentUser, CancellationToken ct)
    {
        var result = await userService.ChangePasswordAsync(currentUser.Id!.Value, request, ct);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
    }
}
