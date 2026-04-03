using Asp.Versioning;
using EntApp.Modules.IAM.Application.Commands;
using EntApp.Modules.IAM.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.IAM;

/// <summary>Rol yönetimi API.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/iam/roles")]
[ApiVersion("1.0")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleController(IMediator mediator) => _mediator = mediator;

    /// <summary>Tüm rolleri getir.</summary>
    [HttpGet]
    [HasPermission("users.read")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRolesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Errors);
    }

    /// <summary>Yeni rol oluştur.</summary>
    [HttpPost]
    [HasPermission("roles.manage")]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/iam/roles/{result.Value}", new { id = result.Value })
            : BadRequest(result.Errors);
    }
}
