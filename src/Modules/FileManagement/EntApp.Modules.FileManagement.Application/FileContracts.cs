using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using FluentValidation;
using MediatR;

namespace EntApp.Modules.FileManagement.Application.DTOs
{
    public sealed record FileEntryDto(
        Guid Id, string FileName, string OriginalFileName, string ContentType,
        long SizeInBytes, string? Description, string? Category,
        int CurrentVersion, bool IsPreviewable, bool IsDeleted,
        Guid? TenantId, IReadOnlyList<string> Tags);

    public sealed record FileVersionDto(
        Guid Id, int VersionNumber, long SizeInBytes, string? ChangeNote, DateTime CreatedAt);
}

namespace EntApp.Modules.FileManagement.Application.Abstractions
{
    /// <summary>Storage provider soyutlaması — local disk, S3, Azure Blob.</summary>
    public interface IStorageProvider
    {
        Task<string> SaveAsync(Stream stream, string fileName, string? subfolder = null, CancellationToken ct = default);
        Task<Stream> GetAsync(string storagePath, CancellationToken ct = default);
        Task DeleteAsync(string storagePath, CancellationToken ct = default);
        Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default);
    }
}

namespace EntApp.Modules.FileManagement.Application.Commands
{
    using DTOs;

    public sealed record UploadFileCommand(
        Stream FileStream, string FileName, string ContentType, long SizeInBytes,
        string? Description = null, string? Category = null,
        Guid? TenantId = null
    ) : IRequest<Result<FileEntryDto>>;

    public sealed record UploadNewVersionCommand(
        Guid FileId, Stream FileStream, string FileName, long SizeInBytes,
        string? ChangeNote = null
    ) : IRequest<Result<FileVersionDto>>;

    public sealed record SoftDeleteFileCommand(Guid FileId) : IRequest<Result>;
    public sealed record RestoreFileCommand(Guid FileId) : IRequest<Result>;

    public sealed record AddTagCommand(Guid FileId, string TagName) : IRequest<Result>;
    public sealed record RemoveTagCommand(Guid FileId, string TagName) : IRequest<Result>;

    public sealed record UpdateFileMetadataCommand(
        Guid FileId, string? Description, string? Category
    ) : IRequest<Result>;
}

namespace EntApp.Modules.FileManagement.Application.Queries
{
    using DTOs;

    public sealed record GetFilesQuery(
        int Page = 1, int PageSize = 20,
        string? Category = null, string? Tag = null,
        bool IncludeDeleted = false, Guid? TenantId = null
    ) : IRequest<Result<PagedResult<FileEntryDto>>>;

    public sealed record GetFileByIdQuery(Guid FileId) : IRequest<Result<FileEntryDto>>;
    public sealed record GetFileVersionsQuery(Guid FileId) : IRequest<Result<IReadOnlyList<FileVersionDto>>>;
    public sealed record DownloadFileQuery(Guid FileId, int? Version = null) : IRequest<Result<(Stream Stream, string FileName, string ContentType)>>;
}

namespace EntApp.Modules.FileManagement.Application.Validators
{
    using Commands;

    public sealed class UploadFileValidator : AbstractValidator<UploadFileCommand>
    {
        private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB

        public UploadFileValidator()
        {
            RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SizeInBytes).GreaterThan(0).LessThanOrEqualTo(MaxFileSize)
                .WithMessage($"Dosya boyutu 100 MB'ı aşamaz.");
        }
    }

    public sealed class AddTagValidator : AbstractValidator<AddTagCommand>
    {
        public AddTagValidator()
        {
            RuleFor(x => x.TagName).NotEmpty().MaximumLength(50)
                .Matches("^[a-zA-Z0-9-_]+$").WithMessage("Tag sadece harf, rakam, tire ve alt çizgi içerebilir.");
        }
    }
}
