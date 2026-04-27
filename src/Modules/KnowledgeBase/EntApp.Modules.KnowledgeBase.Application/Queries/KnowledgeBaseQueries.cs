using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.KnowledgeBase.Application.Queries;

// ── WikiSpace Queries ────────────────────────────────────────────

public sealed record ListWikiSpacesQuery(
    Guid? ProjectId = null) : IRequest<List<WikiSpaceListDto>>;

public sealed record GetWikiSpaceQuery(Guid SpaceId) : IRequest<WikiSpaceDetailDto?>;

// ── WikiPage Queries ─────────────────────────────────────────────

/// <summary>Space içindeki tüm sayfaları hiyerarşik ağaç olarak döndürür.</summary>
public sealed record GetWikiPageTreeQuery(Guid SpaceId) : IRequest<List<WikiPageTreeDto>>;

public sealed record GetWikiPageQuery(Guid PageId) : IRequest<WikiPageDetailDto?>;

public sealed record GetWikiPageBySlugQuery(
    string SpaceSlug, string PageSlug) : IRequest<WikiPageDetailDto?>;

/// <summary>Full-text search — tsvector ile.</summary>
public sealed record SearchWikiPagesQuery(
    string SearchTerm,
    Guid? SpaceId = null, Guid? ProjectId = null,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<WikiPageSearchDto>>;

// ── Versiyon Queries ─────────────────────────────────────────────

public sealed record ListWikiPageVersionsQuery(
    Guid PageId) : IRequest<List<WikiPageVersionDto>>;

public sealed record GetWikiPageVersionQuery(
    Guid VersionId) : IRequest<WikiPageVersionDetailDto?>;

// ── DTOs ─────────────────────────────────────────────────────────

public sealed record WikiSpaceListDto(
    Guid Id, string Name, string Slug, string? Description,
    Guid? ProjectId, string? IconEmoji,
    int PageCount, bool IsActive);

public sealed record WikiSpaceDetailDto(
    Guid Id, string Name, string Slug, string? Description,
    Guid? ProjectId, string? IconEmoji,
    int PageCount, bool IsActive,
    DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record WikiPageTreeDto(
    Guid Id, string Title, string Slug, string Status,
    Guid? ParentPageId, int SortOrder, int ChildCount,
    List<WikiPageTreeDto>? Children = null);

public sealed record WikiPageDetailDto(
    Guid Id, Guid SpaceId, string SpaceName,
    string Title, string Slug,
    string ContentJson, string ContentHtml,
    string Status, int ViewCount,
    Guid LastEditedByUserId,
    Guid? LockedByUserId, DateTime? LockedAt,
    DateTime? PublishedAt,
    Guid? SourceRequirementId, Guid? SourceTicketId,
    DateTime CreatedAt, DateTime? UpdatedAt,
    int VersionCount,
    List<WikiPageBreadcrumbDto>? Breadcrumbs = null);

public sealed record WikiPageBreadcrumbDto(
    Guid Id, string Title, string Slug);

public sealed record WikiPageVersionDto(
    Guid Id, int VersionNumber, string? ChangeNote,
    Guid AuthorUserId, DateTime CreatedAt);

public sealed record WikiPageVersionDetailDto(
    Guid Id, Guid PageId, int VersionNumber,
    string ContentJson, string ContentHtml,
    string? ChangeNote, Guid AuthorUserId, DateTime CreatedAt);

public sealed record WikiPageSearchDto(
    Guid Id, string Title, string Slug,
    string SpaceName, string SpaceSlug,
    string Excerpt, string Status, DateTime UpdatedAt);

// ── Self-service Deflection ──────────────────────────────────
public sealed record SuggestKbArticlesQuery(
    string SearchText, int MaxResults = 5) : IRequest<List<WikiPageSearchDto>>;
