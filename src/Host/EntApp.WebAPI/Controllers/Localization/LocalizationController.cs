using Asp.Versioning;
using EntApp.Modules.Localization.Application.Commands;
using EntApp.Modules.Localization.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.Localization;

[ApiController]
[Route("api/v{version:apiVersion}/localization")]
[ApiVersion("1.0")]
[Authorize]
public class LocalizationController : ControllerBase
{
    private readonly IMediator _mediator;
    public LocalizationController(IMediator mediator) => _mediator = mediator;

    // ─── Languages ──────────────────────────────────

    [HttpGet("languages")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLanguages([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetLanguagesQuery(activeOnly), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    [HttpPost("languages")]
    [HasPermission("localization.manage")]
    public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Created($"/api/v1/localization/languages", new { id = result.Value }) : BadRequest(result.Error);
    }

    [HttpPost("languages/{id:guid}/set-default")]
    [HasPermission("localization.manage")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SetDefaultLanguageCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpPost("languages/{id:guid}/toggle")]
    [HasPermission("localization.manage")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ToggleLanguageCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    // ─── Translations ───────────────────────────────

    [HttpGet("translations/{languageCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTranslations(
        string languageCode, [FromQuery] string? ns = null,
        [FromQuery] Guid? tenantId = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTranslationsQuery(languageCode, ns, tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    /// <summary>Frontend için flat JSON map: { "common.hello": "Merhaba" }</summary>
    [HttpGet("translations/{languageCode}/map")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTranslationMap(
        string languageCode, [FromQuery] string? ns = null,
        [FromQuery] Guid? tenantId = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTranslationMapQuery(languageCode, ns, tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    [HttpGet("translations/by-key/{key}")]
    [HasPermission("localization.read")]
    public async Task<IActionResult> GetByKey(string key, [FromQuery] string? ns = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTranslationsByKeyQuery(key, ns), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }

    [HttpPut("translations")]
    [HasPermission("localization.manage")]
    public async Task<IActionResult> UpsertTranslation([FromBody] UpsertTranslationCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(result.Error);
    }

    [HttpPost("translations/bulk")]
    [HasPermission("localization.manage")]
    public async Task<IActionResult> BulkUpsert([FromBody] BulkUpsertTranslationsCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(new { count = result.Value }) : BadRequest(result.Error);
    }

    [HttpPost("translations/{id:guid}/verify")]
    [HasPermission("localization.manage")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new VerifyTranslationCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpDelete("translations/{id:guid}")]
    [HasPermission("localization.manage")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteTranslationCommand(id), ct);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
