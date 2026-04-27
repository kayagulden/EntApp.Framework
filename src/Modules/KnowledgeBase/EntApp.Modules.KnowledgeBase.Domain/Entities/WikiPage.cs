using EntApp.Modules.KnowledgeBase.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.KnowledgeBase.Domain.Entities;

/// <summary>Wiki sayfası — hiyerarşik, versiyonlanmış, Tiptap JSON + HTML içerik.</summary>
public sealed class WikiPage : AggregateRoot<WikiPageId>, ITenantEntity
{
    public WikiSpaceId WikiSpaceId { get; private set; }

    /// <summary>Üst sayfa — null ise kök sayfa.</summary>
    public WikiPageId? ParentPageId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>URL-friendly tanımlayıcı, space içinde unique.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Tiptap JSON formatında içerik (düzenleme için).</summary>
    public string ContentJson { get; private set; } = "{}";

    /// <summary>Rendered HTML içerik (görüntüleme + arama için).</summary>
    public string ContentHtml { get; private set; } = string.Empty;

    /// <summary>Draft, Published, Archived.</summary>
    public string Status { get; private set; } = WikiPageStatuses.Draft;

    public int SortOrder { get; private set; }
    public int ViewCount { get; private set; }

    public Guid LastEditedByUserId { get; private set; }

    // Concurrent edit koruması
    /// <summary>Sayfayı kilitleyen kullanıcı.</summary>
    public Guid? LockedByUserId { get; private set; }
    /// <summary>Kilit zamanı — 15 dk sonra otomatik sona erer.</summary>
    public DateTime? LockedAt { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    /// <summary>Kaynak requirement ID — requirement'tan üretildiyse.</summary>
    public Guid? SourceRequirementId { get; private set; }

    /// <summary>Kaynak ticket ID — ticket'tan üretildiyse.</summary>
    public Guid? SourceTicketId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public WikiSpace Space { get; private set; } = null!;
    public WikiPage? ParentPage { get; private set; }
    public ICollection<WikiPage> ChildPages { get; private set; } = [];
    public ICollection<WikiPageVersion> Versions { get; private set; } = [];

    private WikiPage() { }

    public static WikiPage Create(WikiSpaceId spaceId, string title, string slug,
        string contentJson, string contentHtml, Guid authorUserId,
        WikiPageId? parentPageId = null, string? status = null,
        Guid? sourceRequirementId = null, Guid? sourceTicketId = null,
        int sortOrder = 0)
    {
        return new WikiPage
        {
            Id = EntityId.New<WikiPageId>(),
            WikiSpaceId = spaceId,
            Title = title,
            Slug = slug.ToLowerInvariant(),
            ContentJson = contentJson,
            ContentHtml = contentHtml,
            Status = status ?? WikiPageStatuses.Draft,
            LastEditedByUserId = authorUserId,
            ParentPageId = parentPageId,
            SourceRequirementId = sourceRequirementId,
            SourceTicketId = sourceTicketId,
            SortOrder = sortOrder
        };
    }

    public void UpdateContent(string contentJson, string contentHtml, Guid editorUserId, string? title = null)
    {
        ContentJson = contentJson;
        ContentHtml = contentHtml;
        LastEditedByUserId = editorUserId;
        if (title is not null) Title = title;
    }

    public void Move(WikiPageId? newParentPageId, int? newSortOrder = null)
    {
        ParentPageId = newParentPageId;
        if (newSortOrder.HasValue) SortOrder = newSortOrder.Value;
    }

    public void Publish()
    {
        Status = WikiPageStatuses.Published;
        PublishedAt ??= DateTime.UtcNow;
    }

    public void Archive() => Status = WikiPageStatuses.Archived;
    public void SetDraft() => Status = WikiPageStatuses.Draft;

    public void IncrementViewCount() => ViewCount++;

    public void SetSortOrder(int order) => SortOrder = order;

    // ── Sayfa Kilitleme ──────────────────────────────────────────
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(15);

    public void Lock(Guid userId)
    {
        LockedByUserId = userId;
        LockedAt = DateTime.UtcNow;
    }

    public void Unlock()
    {
        LockedByUserId = null;
        LockedAt = null;
    }

    public bool IsLockExpired() =>
        LockedAt.HasValue && DateTime.UtcNow - LockedAt.Value > LockTimeout;

    public bool IsLockedByOther(Guid currentUserId) =>
        LockedByUserId.HasValue
        && LockedByUserId.Value != currentUserId
        && !IsLockExpired();
}

/// <summary>Wiki sayfa durumları.</summary>
public static class WikiPageStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";
}
