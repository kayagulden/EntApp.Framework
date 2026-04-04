using Asp.Versioning;
using EntApp.Modules.Notification.Application.Commands;
using EntApp.Modules.Notification.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.Notification;

[ApiController]
[Route("api/v{version:apiVersion}/notifications")]
[ApiVersion("1.0")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;
    public NotificationController(IMediator mediator) => _mediator = mediator;

    // ─── Templates ──────────────────────────────────

    [HttpGet("templates")]
    [HasPermission("notification.read")]
    public async Task<IActionResult> GetTemplates([FromQuery] string? channel = null, [FromQuery] Guid? tenantId = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTemplatesQuery(channel, tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    [HttpPost("templates")]
    [HasPermission("notification.manage")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Created($"/api/v1/notifications/templates/{result.Value}", new { id = result.Value }) : BadRequest(result.Error);
    }

    [HttpPut("templates/{id:guid}")]
    [HasPermission("notification.manage")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { TemplateId = id }, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    // ─── Send ───────────────────────────────────────

    [HttpPost("send")]
    [HasPermission("notification.send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(result.Error);
    }

    // ─── History ────────────────────────────────────

    [HttpGet("history")]
    [HasPermission("notification.read")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null, [FromQuery] string? channel = null,
        [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNotificationHistoryQuery(page, pageSize, userId, channel, status), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromQuery] Guid userId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUnreadCountQuery(userId), ct);
        return result.IsSuccess ? Ok(new { count = result.Value }) : StatusCode(500, result.Error);
    }

    // ─── Preferences ────────────────────────────────

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences([FromQuery] Guid userId, [FromQuery] Guid? tenantId = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUserPreferencesQuery(userId, tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreference([FromBody] UpdatePreferenceCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
