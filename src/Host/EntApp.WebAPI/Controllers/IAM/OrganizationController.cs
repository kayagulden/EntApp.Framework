using Asp.Versioning;
using EntApp.Modules.IAM.Application.Commands;
using EntApp.Modules.IAM.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.IAM;

/// <summary>Organizasyon yönetimi API.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/iam/organizations")]
[ApiVersion("1.0")]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationController(IMediator mediator) => _mediator = mediator;

    /// <summary>Organizasyon ağacını getir.</summary>
    [HttpGet("tree")]
    [HasPermission("organizations.manage")]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationTreeQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    /// <summary>Yeni organizasyon oluştur.</summary>
    [HttpPost]
    [HasPermission("organizations.manage")]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/iam/organizations/{result.Value}", new { id = result.Value })
            : BadRequest(result.Error);
    }
}
