using Asp.Versioning;
using EntApp.Modules.MultiTenancy.Application.Commands;
using EntApp.Modules.MultiTenancy.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.MultiTenancy;

[ApiController]
[Route("api/v{version:apiVersion}/tenants")]
[ApiVersion("1.0")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly IMediator _mediator;
    public TenantController(IMediator mediator) => _mediator = mediator;

    /// <summary>Tüm tenant'ları listele (paginated).</summary>
    [HttpGet]
    [HasPermission("tenant.read")]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] string? plan = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTenantsQuery(page, pageSize, status, plan), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Errors);
    }

    /// <summary>Tenant detayı (ID ile).</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("tenant.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    /// <summary>Tenant detayı (identifier ile).</summary>
    [HttpGet("by-identifier/{identifier}")]
    [HasPermission("tenant.read")]
    public async Task<IActionResult> GetByIdentifier(string identifier, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTenantByIdentifierQuery(identifier), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    /// <summary>Yeni tenant oluştur (+ ITenantSeeder bootstrap).</summary>
    [HttpPost]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Created($"/api/v1/tenants/{result.Value}", new { id = result.Value }) : BadRequest(result.Errors);
    }

    /// <summary>Tenant bilgilerini güncelle.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { TenantId = id }, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Tenant'ı aktifleştir.</summary>
    [HttpPost("{id:guid}/activate")]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ActivateTenantCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Tenant'ı askıya al.</summary>
    [HttpPost("{id:guid}/suspend")]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> Suspend(Guid id, [FromQuery] string? reason = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SuspendTenantCommand(id, reason), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Tenant'ı deaktif et.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeactivateTenantCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Plan değiştir.</summary>
    [HttpPut("{id:guid}/plan")]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> ChangePlan(Guid id, [FromBody] ChangePlanCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { TenantId = id }, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Subdomain ayarla.</summary>
    [HttpPut("{id:guid}/subdomain")]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> SetSubdomain(Guid id, [FromBody] SetSubdomainCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { TenantId = id }, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }

    /// <summary>Tenant ayarı ekle/güncelle.</summary>
    [HttpPut("{id:guid}/settings")]
    [HasPermission("tenant.manage")]
    public async Task<IActionResult> UpsertSetting(Guid id, [FromBody] UpsertTenantSettingCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { TenantId = id }, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }
}
