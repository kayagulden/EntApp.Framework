using MediatR;

namespace EntApp.Modules.KnowledgeBase.Application.Commands;

// ── WikiSpace ────────────────────────────────────────────────────

public sealed record CreateWikiSpaceCommand(
    string Name, string Slug,
    string? Description = null, Guid? ProjectId = null,
    string? IconEmoji = null) : IRequest<Guid>;

public sealed record UpdateWikiSpaceCommand(
    Guid SpaceId,
    string? Name = null, string? Description = null,
    string? IconEmoji = null) : IRequest;

public sealed record DeleteWikiSpaceCommand(Guid SpaceId) : IRequest;

// ── WikiPage ─────────────────────────────────────────────────────

public sealed record CreateWikiPageCommand(
    Guid SpaceId, string Title,
    string ContentJson, string ContentHtml,
    Guid? ParentPageId = null,
    string? Status = null,
    Guid? SourceRequirementId = null,
    Guid? SourceTicketId = null) : IRequest<Guid>;

public sealed record UpdateWikiPageCommand(
    Guid PageId,
    string? Title = null,
    string? ContentJson = null,
    string? ContentHtml = null,
    string? ChangeNote = null) : IRequest<Guid>;

public sealed record MoveWikiPageCommand(
    Guid PageId,
    Guid? NewParentPageId = null,
    int? NewSortOrder = null) : IRequest;

public sealed record PublishWikiPageCommand(Guid PageId) : IRequest;
public sealed record ArchiveWikiPageCommand(Guid PageId) : IRequest;
public sealed record DeleteWikiPageCommand(Guid PageId) : IRequest;

public sealed record LockWikiPageCommand(Guid PageId) : IRequest;
public sealed record UnlockWikiPageCommand(Guid PageId) : IRequest;

// ── Versiyon ─────────────────────────────────────────────────────

/// <summary>Belirli bir versiyona geri döndürür — yeni versiyon oluşturarak.</summary>
public sealed record RevertToVersionCommand(Guid PageId, Guid VersionId) : IRequest<Guid>;

// ── AI / Entegrasyon ─────────────────────────────────────────

/// <summary>
/// FeatureSpec requirement + altındaki tüm child gereksinimlerden
/// yapılandırılmış bir wiki spec dokümanı üretir.
/// </summary>
public sealed record GenerateWikiFromRequirementCommand(
    Guid RequirementId, Guid ProjectId) : IRequest<Guid>;

/// <summary>
/// Çözülmüş bir ticket'tan KB taslağı üretir (AI destekli).
/// TicketResolvedEvent handler'ı tarafından otomatik tetiklenir.
/// </summary>
public sealed record GenerateKbFromTicketCommand(
    Guid TicketId, string TicketNumber,
    string Title, string? Description, string? Resolution,
    Guid? CategoryId, Guid? AssigneeUserId) : IRequest<Guid>;
