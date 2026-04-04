using EntApp.Modules.FileManagement.Application.Abstractions;
using EntApp.Modules.FileManagement.Application.Commands;
using EntApp.Modules.FileManagement.Application.DTOs;
using EntApp.Modules.FileManagement.Application.Queries;
using EntApp.Modules.FileManagement.Domain.Entities;
using EntApp.Modules.FileManagement.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.FileManagement.Infrastructure.Handlers;

// ─── Upload ─────────────────────────────────────────
public sealed class UploadFileHandler : IRequestHandler<UploadFileCommand, Result<FileEntryDto>>
{
    private readonly FileDbContext _db;
    private readonly IStorageProvider _storage;
    public UploadFileHandler(FileDbContext db, IStorageProvider storage) { _db = db; _storage = storage; }

    public async Task<Result<FileEntryDto>> Handle(UploadFileCommand req, CancellationToken ct)
    {
        var storagePath = await _storage.SaveAsync(req.FileStream, req.FileName, req.Category, ct);
        var file = FileEntry.Create(req.FileName, req.FileName, req.ContentType, req.SizeInBytes, storagePath, req.Description, req.Category, req.TenantId);

        _db.Files.Add(file);
        await _db.SaveChangesAsync(ct);

        return Result<FileEntryDto>.Success(FileMappers.MapToDto(file));
    }
}

// ─── UploadNewVersion ───────────────────────────────
public sealed class UploadNewVersionHandler : IRequestHandler<UploadNewVersionCommand, Result<FileVersionDto>>
{
    private readonly FileDbContext _db;
    private readonly IStorageProvider _storage;
    public UploadNewVersionHandler(FileDbContext db, IStorageProvider storage) { _db = db; _storage = storage; }

    public async Task<Result<FileVersionDto>> Handle(UploadNewVersionCommand req, CancellationToken ct)
    {
        var file = await _db.Files.FindAsync([req.FileId], ct);
        if (file is null) return Result<FileVersionDto>.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));

        var storagePath = await _storage.SaveAsync(req.FileStream, req.FileName, ct: ct);
        var version = file.AddVersion(storagePath, req.SizeInBytes, req.ChangeNote);
        await _db.SaveChangesAsync(ct);

        return Result<FileVersionDto>.Success(new FileVersionDto(version.Id, version.VersionNumber, version.SizeInBytes, version.ChangeNote, version.CreatedAt));
    }
}

// ─── SoftDelete ─────────────────────────────────────
public sealed class SoftDeleteHandler : IRequestHandler<SoftDeleteFileCommand, Result>
{
    private readonly FileDbContext _db;
    public SoftDeleteHandler(FileDbContext db) => _db = db;

    public async Task<Result> Handle(SoftDeleteFileCommand req, CancellationToken ct)
    {
        var file = await _db.Files.FindAsync([req.FileId], ct);
        if (file is null) return Result.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));
        file.SoftDelete();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Restore ────────────────────────────────────────
public sealed class RestoreFileHandler : IRequestHandler<RestoreFileCommand, Result>
{
    private readonly FileDbContext _db;
    public RestoreFileHandler(FileDbContext db) => _db = db;

    public async Task<Result> Handle(RestoreFileCommand req, CancellationToken ct)
    {
        var file = await _db.Files.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == req.FileId, ct);
        if (file is null) return Result.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));
        file.Restore();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── AddTag ─────────────────────────────────────────
public sealed class AddTagHandler : IRequestHandler<AddTagCommand, Result>
{
    private readonly FileDbContext _db;
    public AddTagHandler(FileDbContext db) => _db = db;

    public async Task<Result> Handle(AddTagCommand req, CancellationToken ct)
    {
        var file = await _db.Files.Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == req.FileId, ct);
        if (file is null) return Result.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));
        file.AddTag(req.TagName);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── RemoveTag ──────────────────────────────────────
public sealed class RemoveTagHandler : IRequestHandler<RemoveTagCommand, Result>
{
    private readonly FileDbContext _db;
    public RemoveTagHandler(FileDbContext db) => _db = db;

    public async Task<Result> Handle(RemoveTagCommand req, CancellationToken ct)
    {
        var file = await _db.Files.Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == req.FileId, ct);
        if (file is null) return Result.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));
        file.RemoveTag(req.TagName);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── UpdateMetadata ─────────────────────────────────
public sealed class UpdateMetadataHandler : IRequestHandler<UpdateFileMetadataCommand, Result>
{
    private readonly FileDbContext _db;
    public UpdateMetadataHandler(FileDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateFileMetadataCommand req, CancellationToken ct)
    {
        var file = await _db.Files.FindAsync([req.FileId], ct);
        if (file is null) return Result.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));
        file.UpdateMetadata(req.Description, req.Category);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── GetFiles ───────────────────────────────────────
public sealed class GetFilesHandler : IRequestHandler<GetFilesQuery, Result<PagedResult<FileEntryDto>>>
{
    private readonly FileDbContext _db;
    public GetFilesHandler(FileDbContext db) => _db = db;

    public async Task<Result<PagedResult<FileEntryDto>>> Handle(GetFilesQuery req, CancellationToken ct)
    {
        var query = _db.Files.AsNoTracking().Include(f => f.Tags).AsQueryable();
        if (req.IncludeDeleted) query = query.IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(req.Category)) query = query.Where(f => f.Category == req.Category);
        if (req.TenantId.HasValue) query = query.Where(f => f.TenantId == req.TenantId);
        if (!string.IsNullOrWhiteSpace(req.Tag)) query = query.Where(f => f.Tags.Any(t => t.Name == req.Tag.ToLowerInvariant()));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(f => f.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(FileMappers.MapToDto).ToList();
        return Result<PagedResult<FileEntryDto>>.Success(new PagedResult<FileEntryDto> { Items = dtos, TotalCount = total, PageNumber = req.Page, PageSize = req.PageSize });
    }
}

// ─── GetFileById ────────────────────────────────────
public sealed class GetFileByIdHandler : IRequestHandler<GetFileByIdQuery, Result<FileEntryDto>>
{
    private readonly FileDbContext _db;
    public GetFileByIdHandler(FileDbContext db) => _db = db;

    public async Task<Result<FileEntryDto>> Handle(GetFileByIdQuery req, CancellationToken ct)
    {
        var file = await _db.Files.AsNoTracking().Include(f => f.Tags).FirstOrDefaultAsync(f => f.Id == req.FileId, ct);
        if (file is null) return Result<FileEntryDto>.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));
        return Result<FileEntryDto>.Success(FileMappers.MapToDto(file));
    }
}

// ─── GetFileVersions ────────────────────────────────
public sealed class GetVersionsHandler : IRequestHandler<GetFileVersionsQuery, Result<IReadOnlyList<FileVersionDto>>>
{
    private readonly FileDbContext _db;
    public GetVersionsHandler(FileDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<FileVersionDto>>> Handle(GetFileVersionsQuery req, CancellationToken ct)
    {
        var items = await _db.Versions.AsNoTracking()
            .Where(v => v.FileEntryId == req.FileId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new FileVersionDto(v.Id, v.VersionNumber, v.SizeInBytes, v.ChangeNote, v.CreatedAt))
            .ToListAsync(ct);
        return Result<IReadOnlyList<FileVersionDto>>.Success(items);
    }
}

// ─── Download ───────────────────────────────────────
public sealed class DownloadFileHandler : IRequestHandler<DownloadFileQuery, Result<(Stream Stream, string FileName, string ContentType)>>
{
    private readonly FileDbContext _db;
    private readonly IStorageProvider _storage;
    public DownloadFileHandler(FileDbContext db, IStorageProvider storage) { _db = db; _storage = storage; }

    public async Task<Result<(Stream Stream, string FileName, string ContentType)>> Handle(DownloadFileQuery req, CancellationToken ct)
    {
        var file = await _db.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == req.FileId, ct);
        if (file is null) return Result<(Stream, string, string)>.Failure(Error.NotFound("File.NotFound", "Dosya bulunamadı."));

        var storagePath = file.StoragePath;
        if (req.Version.HasValue)
        {
            var version = await _db.Versions.AsNoTracking()
                .FirstOrDefaultAsync(v => v.FileEntryId == req.FileId && v.VersionNumber == req.Version.Value, ct);
            if (version is not null) storagePath = version.StoragePath;
        }

        var stream = await _storage.GetAsync(storagePath, ct);
        return Result<(Stream, string, string)>.Success((stream, file.OriginalFileName, file.ContentType));
    }
}

// ─── Helper ─────────────────────────────────────────
internal static class FileMappers
{
    public static FileEntryDto MapToDto(FileEntry f) => new(
        f.Id, f.FileName, f.OriginalFileName, f.ContentType,
        f.SizeInBytes, f.Description, f.Category,
        f.CurrentVersion, f.IsPreviewable(), f.IsDeleted,
        f.TenantId, f.Tags.Select(t => t.Name).ToList());
}
