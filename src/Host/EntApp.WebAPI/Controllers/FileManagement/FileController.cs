using Asp.Versioning;
using EntApp.Modules.FileManagement.Application.Commands;
using EntApp.Modules.FileManagement.Application.Queries;
using EntApp.Shared.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EntApp.WebAPI.Controllers.FileManagement;

[ApiController]
[Route("api/v{version:apiVersion}/files")]
[ApiVersion("1.0")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IMediator _mediator;
    public FileController(IMediator mediator) => _mediator = mediator;

    /// <summary>Dosya yükle.</summary>
    [HttpPost("upload")]
    [HasPermission("file.upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] string? description = null,
        [FromQuery] string? category = null,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadFileCommand(
            stream, file.FileName, file.ContentType, file.Length,
            description, category, tenantId), ct);

        return result.IsSuccess ? Created($"/api/v1/files/{result.Value.Id}", result.Value) : BadRequest(result.Errors);
    }

    /// <summary>Yeni versiyon yükle.</summary>
    [HttpPost("{id:guid}/versions")]
    [HasPermission("file.upload")]
    public async Task<IActionResult> UploadVersion(
        Guid id, IFormFile file, [FromQuery] string? changeNote = null, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadNewVersionCommand(id, stream, file.FileName, file.Length, changeNote), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    /// <summary>Dosyaları listele (sayfalanmış + filtre).</summary>
    [HttpGet]
    [HasPermission("file.read")]
    public async Task<IActionResult> GetFiles(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null, [FromQuery] string? tag = null,
        [FromQuery] bool includeDeleted = false, [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFilesQuery(page, pageSize, category, tag, includeDeleted, tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Errors);
    }

    /// <summary>Dosya detayı.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("file.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFileByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    /// <summary>Dosya versiyonları.</summary>
    [HttpGet("{id:guid}/versions")]
    [HasPermission("file.read")]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFileVersionsQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors);
    }

    /// <summary>Dosya indir (opsiyonel versiyon).</summary>
    [HttpGet("{id:guid}/download")]
    [HasPermission("file.read")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] int? version = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DownloadFileQuery(id, version), ct);
        if (!result.IsSuccess) return NotFound(result.Errors);
        return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>Dosya sil (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("file.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SoftDeleteFileCommand(id), ct);
        return result.IsSuccess ? NoContent() : NotFound(result.Errors);
    }

    /// <summary>Silinen dosyayı geri yükle.</summary>
    [HttpPost("{id:guid}/restore")]
    [HasPermission("file.delete")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new RestoreFileCommand(id), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Tag ekle.</summary>
    [HttpPost("{id:guid}/tags")]
    [HasPermission("file.upload")]
    public async Task<IActionResult> AddTag(Guid id, [FromBody] AddTagCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { FileId = id }, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Tag kaldır.</summary>
    [HttpDelete("{id:guid}/tags/{tagName}")]
    [HasPermission("file.upload")]
    public async Task<IActionResult> RemoveTag(Guid id, string tagName, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new RemoveTagCommand(id, tagName), ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }

    /// <summary>Metadata güncelle.</summary>
    [HttpPut("{id:guid}/metadata")]
    [HasPermission("file.upload")]
    public async Task<IActionResult> UpdateMetadata(Guid id, [FromBody] UpdateFileMetadataCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command with { FileId = id }, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Errors);
    }
}
