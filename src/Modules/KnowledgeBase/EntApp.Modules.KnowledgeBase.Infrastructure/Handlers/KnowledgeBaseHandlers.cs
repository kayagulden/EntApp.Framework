using EntApp.Modules.KnowledgeBase.Application.Commands;
using EntApp.Modules.KnowledgeBase.Application.Queries;
using EntApp.Modules.KnowledgeBase.Domain.Entities;
using EntApp.Modules.KnowledgeBase.Domain.Ids;
using EntApp.Modules.KnowledgeBase.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace EntApp.Modules.KnowledgeBase.Infrastructure.Handlers;

// ══════════════════════════════════════════════════════════════
// WIKISPACE HANDLERS
// ══════════════════════════════════════════════════════════════

// ── WikiSpace Queries ────────────────────────────────────────
public sealed class ListWikiSpacesQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<ListWikiSpacesQuery, List<WikiSpaceListDto>>
{
    public async Task<List<WikiSpaceListDto>> Handle(ListWikiSpacesQuery request, CancellationToken ct)
    {
        var query = db.WikiSpaces.AsQueryable();
        if (request.ProjectId.HasValue)
            query = query.Where(s => s.ProjectId == request.ProjectId.Value);
        return await query.OrderBy(s => s.Name)
            .Select(s => new WikiSpaceListDto(
                s.Id.Value, s.Name, s.Slug, s.Description,
                s.ProjectId, s.IconEmoji,
                s.Pages.Count(p => !p.IsDeleted), s.IsActive))
            .ToListAsync(ct);
    }
}

public sealed class GetWikiSpaceQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<GetWikiSpaceQuery, WikiSpaceDetailDto?>
{
    public async Task<WikiSpaceDetailDto?> Handle(GetWikiSpaceQuery request, CancellationToken ct)
    {
        var s = await db.WikiSpaces.FirstOrDefaultAsync(x => x.Id.Value == request.SpaceId, ct);
        if (s is null) return null;
        var pageCount = await db.WikiPages.CountAsync(p => p.WikiSpaceId == s.Id, ct);
        return new WikiSpaceDetailDto(
            s.Id.Value, s.Name, s.Slug, s.Description,
            s.ProjectId, s.IconEmoji, pageCount, s.IsActive,
            s.CreatedAt, s.UpdatedAt);
    }
}

// ── WikiSpace Commands ───────────────────────────────────────
public sealed class CreateWikiSpaceCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<CreateWikiSpaceCommand, Guid>
{
    public async Task<Guid> Handle(CreateWikiSpaceCommand request, CancellationToken ct)
    {
        var space = WikiSpace.Create(request.Name, request.Slug,
            request.Description, request.ProjectId, request.IconEmoji);
        db.WikiSpaces.Add(space);
        await db.SaveChangesAsync(ct);
        return space.Id.Value;
    }
}

public sealed class UpdateWikiSpaceCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<UpdateWikiSpaceCommand>
{
    public async Task Handle(UpdateWikiSpaceCommand request, CancellationToken ct)
    {
        var space = await db.WikiSpaces.FindAsync([new WikiSpaceId(request.SpaceId)], ct)
            ?? throw new KeyNotFoundException($"WikiSpace {request.SpaceId} not found");
        space.Update(request.Name, request.Description, request.IconEmoji);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class DeleteWikiSpaceCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<DeleteWikiSpaceCommand>
{
    public async Task Handle(DeleteWikiSpaceCommand request, CancellationToken ct)
    {
        var space = await db.WikiSpaces.FindAsync([new WikiSpaceId(request.SpaceId)], ct)
            ?? throw new KeyNotFoundException($"WikiSpace {request.SpaceId} not found");
        space.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }
}

// ══════════════════════════════════════════════════════════════
// WIKIPAGE HANDLERS
// ══════════════════════════════════════════════════════════════

// ── WikiPage Queries ─────────────────────────────────────────
public sealed class GetWikiPageTreeQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<GetWikiPageTreeQuery, List<WikiPageTreeDto>>
{
    public async Task<List<WikiPageTreeDto>> Handle(GetWikiPageTreeQuery request, CancellationToken ct)
    {
        var spaceId = new WikiSpaceId(request.SpaceId);
        var allPages = await db.WikiPages
            .Where(p => p.WikiSpaceId == spaceId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .Select(p => new
            {
                p.Id, p.Title, p.Slug, p.Status,
                ParentPageId = p.ParentPageId.HasValue ? p.ParentPageId.Value.Value : (Guid?)null,
                p.SortOrder,
                ChildCount = p.ChildPages.Count(c => !c.IsDeleted)
            })
            .ToListAsync(ct);

        // Hiyerarşik ağaç oluştur
        var lookup = allPages.ToLookup(p => p.ParentPageId);

        List<WikiPageTreeDto> BuildTree(Guid? parentId)
        {
            return lookup[parentId]
                .Select(p => new WikiPageTreeDto(
                    p.Id.Value, p.Title, p.Slug, p.Status,
                    p.ParentPageId.HasValue ? p.ParentPageId : null,
                    p.SortOrder, p.ChildCount,
                    BuildTree(p.Id.Value)))
                .ToList();
        }

        return BuildTree(null);
    }
}

public sealed class GetWikiPageQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<GetWikiPageQuery, WikiPageDetailDto?>
{
    public async Task<WikiPageDetailDto?> Handle(GetWikiPageQuery request, CancellationToken ct)
    {
        var pageId = new WikiPageId(request.PageId);
        var p = await db.WikiPages
            .Include(x => x.Space)
            .FirstOrDefaultAsync(x => x.Id == pageId, ct);
        if (p is null) return null;

        var versionCount = await db.WikiPageVersions.CountAsync(v => v.WikiPageId == pageId, ct);
        var breadcrumbs = await BuildBreadcrumbs(p, ct);

        // Görüntülenme sayısını artır
        p.IncrementViewCount();
        await db.SaveChangesAsync(ct);

        return new WikiPageDetailDto(
            p.Id.Value, p.WikiSpaceId.Value, p.Space.Name,
            p.Title, p.Slug,
            p.ContentJson, p.ContentHtml,
            p.Status, p.ViewCount,
            p.LastEditedByUserId,
            p.LockedByUserId, p.LockedAt,
            p.PublishedAt,
            p.SourceRequirementId, p.SourceTicketId,
            p.CreatedAt, p.UpdatedAt,
            versionCount, breadcrumbs);
    }

    private async Task<List<WikiPageBreadcrumbDto>> BuildBreadcrumbs(WikiPage page, CancellationToken ct)
    {
        var breadcrumbs = new List<WikiPageBreadcrumbDto>();
        var currentPageId = page.ParentPageId;

        while (currentPageId.HasValue)
        {
            var parent = await db.WikiPages
                .Where(p => p.Id == currentPageId.Value)
                .Select(p => new { p.Id, p.Title, p.Slug, p.ParentPageId })
                .FirstOrDefaultAsync(ct);
            if (parent is null) break;
            breadcrumbs.Insert(0, new WikiPageBreadcrumbDto(parent.Id.Value, parent.Title, parent.Slug));
            currentPageId = parent.ParentPageId;
        }

        return breadcrumbs;
    }
}

public sealed class GetWikiPageBySlugQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<GetWikiPageBySlugQuery, WikiPageDetailDto?>
{
    public async Task<WikiPageDetailDto?> Handle(GetWikiPageBySlugQuery request, CancellationToken ct)
    {
        var space = await db.WikiSpaces
            .FirstOrDefaultAsync(s => s.Slug == request.SpaceSlug, ct);
        if (space is null) return null;

        var page = await db.WikiPages
            .FirstOrDefaultAsync(p => p.WikiSpaceId == space.Id && p.Slug == request.PageSlug, ct);
        if (page is null) return null;

        // GetWikiPageQuery handler'ına delege et
        var handler = new GetWikiPageQueryHandler(db);
        return await handler.Handle(new GetWikiPageQuery(page.Id.Value), ct);
    }
}

public sealed class SearchWikiPagesQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<SearchWikiPagesQuery, PagedResult<WikiPageSearchDto>>
{
    public async Task<PagedResult<WikiPageSearchDto>> Handle(SearchWikiPagesQuery request, CancellationToken ct)
    {
        var query = db.WikiPages
            .Include(p => p.Space)
            .Where(p => p.Status == WikiPageStatuses.Published);

        if (request.SpaceId.HasValue)
        {
            var spaceId = new WikiSpaceId(request.SpaceId.Value);
            query = query.Where(p => p.WikiSpaceId == spaceId);
        }

        if (request.ProjectId.HasValue)
            query = query.Where(p => p.Space.ProjectId == request.ProjectId.Value);

        // Basit LIKE araması — Faz C'de tsvector ile değiştirilecek
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLowerInvariant();
            query = query.Where(p =>
                p.Title.ToLower().Contains(term) ||
                p.ContentHtml.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new WikiPageSearchDto(
                p.Id.Value, p.Title, p.Slug,
                p.Space.Name, p.Space.Slug,
                p.ContentHtml.Length > 200 ? p.ContentHtml.Substring(0, 200) + "..." : p.ContentHtml,
                p.Status,
                p.UpdatedAt ?? p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WikiPageSearchDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }
}

// ── WikiPage Commands ────────────────────────────────────────
public sealed class CreateWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<CreateWikiPageCommand, Guid>
{
    public async Task<Guid> Handle(CreateWikiPageCommand request, CancellationToken ct)
    {
        var spaceId = new WikiSpaceId(request.SpaceId);

        // Slug üret — title'dan
        var slug = GenerateSlug(request.Title);

        // Slug uniqueness check within space
        var slugExists = await db.WikiPages.AnyAsync(
            p => p.WikiSpaceId == spaceId && p.Slug == slug, ct);
        if (slugExists)
            slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var page = WikiPage.Create(spaceId, request.Title, slug,
            request.ContentJson, request.ContentHtml,
            Guid.Empty, // TODO: ICurrentUser'dan alınacak
            request.ParentPageId.HasValue ? new WikiPageId(request.ParentPageId.Value) : null,
            request.Status,
            request.SourceRequirementId,
            request.SourceTicketId);

        db.WikiPages.Add(page);

        // İlk versiyon oluştur
        var version = WikiPageVersion.Create(page.Id, 1,
            request.ContentJson, request.ContentHtml,
            Guid.Empty, "İlk oluşturma");
        db.WikiPageVersions.Add(version);

        await db.SaveChangesAsync(ct);
        return page.Id.Value;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace("ş", "s").Replace("ç", "c").Replace("ğ", "g")
            .Replace("ü", "u").Replace("ö", "o").Replace("ı", "i")
            .Replace("Ş", "s").Replace("Ç", "c").Replace("Ğ", "g")
            .Replace("Ü", "u").Replace("Ö", "o").Replace("İ", "i");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}

public sealed class UpdateWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<UpdateWikiPageCommand, Guid>
{
    public async Task<Guid> Handle(UpdateWikiPageCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");

        // Kilit kontrolü
        if (page.IsLockedByOther(Guid.Empty)) // TODO: ICurrentUser
            throw new InvalidOperationException("Sayfa başka bir kullanıcı tarafından düzenleniyor.");

        // İçerik değiştiyse yeni versiyon oluştur
        if (request.ContentJson is not null && request.ContentHtml is not null)
        {
            var lastVersion = await db.WikiPageVersions
                .Where(v => v.WikiPageId == page.Id)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => v.VersionNumber)
                .FirstOrDefaultAsync(ct);

            var version = WikiPageVersion.Create(page.Id, lastVersion + 1,
                request.ContentJson, request.ContentHtml,
                Guid.Empty, // TODO: ICurrentUser
                request.ChangeNote);
            db.WikiPageVersions.Add(version);
        }

        page.UpdateContent(
            request.ContentJson ?? page.ContentJson,
            request.ContentHtml ?? page.ContentHtml,
            Guid.Empty, // TODO: ICurrentUser
            request.Title);

        await db.SaveChangesAsync(ct);
        return page.Id.Value;
    }
}

public sealed class MoveWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<MoveWikiPageCommand>
{
    public async Task Handle(MoveWikiPageCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");
        page.Move(
            request.NewParentPageId.HasValue ? new WikiPageId(request.NewParentPageId.Value) : null,
            request.NewSortOrder);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class PublishWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<PublishWikiPageCommand>
{
    public async Task Handle(PublishWikiPageCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");
        page.Publish();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ArchiveWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<ArchiveWikiPageCommand>
{
    public async Task Handle(ArchiveWikiPageCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");
        page.Archive();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class DeleteWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<DeleteWikiPageCommand>
{
    public async Task Handle(DeleteWikiPageCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");
        page.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }
}

public sealed class LockWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<LockWikiPageCommand>
{
    public async Task Handle(LockWikiPageCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");

        if (page.IsLockedByOther(Guid.Empty)) // TODO: ICurrentUser
            throw new InvalidOperationException("Sayfa başka bir kullanıcı tarafından kilitlenmiş.");

        page.Lock(Guid.Empty); // TODO: ICurrentUser
        await db.SaveChangesAsync(ct);
    }
}

public sealed class UnlockWikiPageCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<UnlockWikiPageCommand>
{
    public async Task Handle(UnlockWikiPageCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");
        page.Unlock();
        await db.SaveChangesAsync(ct);
    }
}

// ── Version Queries ──────────────────────────────────────────
public sealed class ListWikiPageVersionsQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<ListWikiPageVersionsQuery, List<WikiPageVersionDto>>
{
    public async Task<List<WikiPageVersionDto>> Handle(ListWikiPageVersionsQuery request, CancellationToken ct)
    {
        var pageId = new WikiPageId(request.PageId);
        return await db.WikiPageVersions
            .Where(v => v.WikiPageId == pageId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new WikiPageVersionDto(
                v.Id.Value, v.VersionNumber, v.ChangeNote,
                v.AuthorUserId, v.CreatedAt))
            .ToListAsync(ct);
    }
}

public sealed class GetWikiPageVersionQueryHandler(KnowledgeBaseDbContext db) : IRequestHandler<GetWikiPageVersionQuery, WikiPageVersionDetailDto?>
{
    public async Task<WikiPageVersionDetailDto?> Handle(GetWikiPageVersionQuery request, CancellationToken ct)
    {
        var v = await db.WikiPageVersions
            .FirstOrDefaultAsync(x => x.Id.Value == request.VersionId, ct);
        if (v is null) return null;
        return new WikiPageVersionDetailDto(
            v.Id.Value, v.WikiPageId.Value, v.VersionNumber,
            v.ContentJson, v.ContentHtml,
            v.ChangeNote, v.AuthorUserId, v.CreatedAt);
    }
}

// ── Revert Command ───────────────────────────────────────────
public sealed class RevertToVersionCommandHandler(KnowledgeBaseDbContext db) : IRequestHandler<RevertToVersionCommand, Guid>
{
    public async Task<Guid> Handle(RevertToVersionCommand request, CancellationToken ct)
    {
        var page = await db.WikiPages.FindAsync([new WikiPageId(request.PageId)], ct)
            ?? throw new KeyNotFoundException($"WikiPage {request.PageId} not found");

        var version = await db.WikiPageVersions
            .FirstOrDefaultAsync(v => v.Id.Value == request.VersionId && v.WikiPageId == page.Id, ct)
            ?? throw new KeyNotFoundException($"Version {request.VersionId} not found");

        // Mevcut halinden yeni versiyon oluştur (geri dönüş kaydı)
        var lastVersion = await db.WikiPageVersions
            .Where(v => v.WikiPageId == page.Id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        var newVersion = WikiPageVersion.Create(page.Id, lastVersion + 1,
            version.ContentJson, version.ContentHtml,
            Guid.Empty, // TODO: ICurrentUser
            $"v{version.VersionNumber} versiyonuna geri döndürüldü");
        db.WikiPageVersions.Add(newVersion);

        // Sayfa içeriğini eski versiyona güncelle
        page.UpdateContent(version.ContentJson, version.ContentHtml, Guid.Empty);

        await db.SaveChangesAsync(ct);
        return page.Id.Value;
    }
}
