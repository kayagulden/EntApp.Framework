using Asp.Versioning;
using EntApp.Modules.Configuration.Application.Commands;
using EntApp.Modules.Configuration.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.Configuration;

/// <summary>Konfigürasyon ve feature flag yönetimi API.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/config")]
[ApiVersion("1.0")]
[Authorize]
public class ConfigController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigController(IMediator mediator) => _mediator = mediator;

    // ─── AppSettings ────────────────────────────────

    /// <summary>Tüm ayarları getir (grup + tenant filtre).</summary>
    [HttpGet("settings")]
    [HasPermission("config.read")]
    public async Task<IActionResult> GetSettings(
        [FromQuery] string? group = null,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAppSettingsQuery(group, tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Errors);
    }

    /// <summary>Tek ayar — key ile.</summary>
    [HttpGet("settings/{key}")]
    [HasPermission("config.read")]
    public async Task<IActionResult> GetSettingByKey(
        string key,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAppSettingByKeyQuery(key, tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    /// <summary>Ayar oluştur/güncelle (upsert).</summary>
    [HttpPut("settings")]
    [HasPermission("config.manage")]
    public async Task<IActionResult> UpsertSetting(
        [FromBody] UpsertAppSettingCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(result.Errors);
    }

    // ─── Feature Flags ──────────────────────────────

    /// <summary>Tüm feature flag'leri getir.</summary>
    [HttpGet("flags")]
    [HasPermission("config.read")]
    public async Task<IActionResult> GetFlags(
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeatureFlagsQuery(tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Errors);
    }

    /// <summary>Feature flag aktif mi?</summary>
    [HttpGet("flags/{name}/enabled")]
    [HasPermission("config.read")]
    public async Task<IActionResult> IsEnabled(
        string name,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new IsFeatureEnabledQuery(name, tenantId), ct);
        return result.IsSuccess ? Ok(new { enabled = result.Value }) : StatusCode(500, result.Errors);
    }

    /// <summary>Yeni feature flag oluştur.</summary>
    [HttpPost("flags")]
    [HasPermission("config.manage")]
    public async Task<IActionResult> CreateFlag(
        [FromBody] CreateFeatureFlagCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Created($"/api/v1/config/flags/{result.Value}", new { id = result.Value })
            : BadRequest(result.Errors);
    }

    /// <summary>Feature flag toggle (aç/kapa).</summary>
    [HttpPost("flags/{id:guid}/toggle")]
    [HasPermission("config.manage")]
    public async Task<IActionResult> ToggleFlag(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ToggleFeatureFlagCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Feature flag zamanlama ayarla.</summary>
    [HttpPut("flags/{id:guid}/schedule")]
    [HasPermission("config.manage")]
    public async Task<IActionResult> SetSchedule(
        Guid id,
        [FromBody] SetFeatureFlagScheduleCommand command,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { FlagId = id }, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }
}
