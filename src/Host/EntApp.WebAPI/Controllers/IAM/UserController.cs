using Asp.Versioning;
using EntApp.Modules.IAM.Application.Commands;
using EntApp.Modules.IAM.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.IAM;

/// <summary>Kullanıcı yönetimi API.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/iam/users")]
[ApiVersion("1.0")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) => _mediator = mediator;

    /// <summary>Kullanıcı listesi (sayfalanmış).</summary>
    [HttpGet]
    [HasPermission("users.read")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetUsersPagedQuery(page, pageSize, search, status), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Kullanıcı detayı.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("users.read")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Yeni kullanıcı oluştur.</summary>
    [HttpPost]
    [HasPermission("users.create")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUser), new { id = result.Value }, new { id = result.Value })
            : BadRequest(result.Error);
    }

    /// <summary>Kullanıcı güncelle.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission("users.update")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.UserId)
        {
            return BadRequest("URL ve body ID'leri eşleşmiyor.");
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    /// <summary>Kullanıcıya rol ata.</summary>
    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    [HasPermission("roles.manage")]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignRoleCommand(userId, roleId), cancellationToken);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    /// <summary>Kullanıcıyı deaktif et.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [HasPermission("users.update")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivateUserCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
