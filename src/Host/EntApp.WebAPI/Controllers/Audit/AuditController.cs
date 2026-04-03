using Asp.Versioning;
using EntApp.Modules.Audit.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.Audit;

/// <summary>Denetim günlükleri API.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/audit")]
[ApiVersion("1.0")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator) => _mediator = mediator;

    /// <summary>Audit logları (sayfalanmış + filtreleme).</summary>
    [HttpGet("logs")]
    [HasPermission("audit.read")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(page, pageSize, entityType, action, userId, fromDate, toDate), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Errors);
    }

    /// <summary>Login kayıtları (sayfalanmış + filtreleme).</summary>
    [HttpGet("logins")]
    [HasPermission("audit.read")]
    public async Task<IActionResult> GetLoginRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? loginResult = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetLoginRecordsQuery(page, pageSize, userId, loginResult, fromDate, toDate), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Errors);
    }
}
