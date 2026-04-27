using EntApp.Modules.KnowledgeBase.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.KnowledgeBase.Domain.Entities;

/// <summary>Wiki sayfa versiyon geçmişi — her güncelleme bir snapshot kaydeder.</summary>
public sealed class WikiPageVersion : AuditableEntity<WikiPageVersionId>, ITenantEntity
{
    public WikiPageId WikiPageId { get; private set; }

    /// <summary>Sayfa bazlı otomatik artan versiyon numarası.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>O anki Tiptap JSON snapshot.</summary>
    public string ContentJson { get; private set; } = "{}";

    /// <summary>O anki rendered HTML snapshot.</summary>
    public string ContentHtml { get; private set; } = string.Empty;

    /// <summary>Değişiklik notu — "Bölüm 3 güncellendi".</summary>
    public string? ChangeNote { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public WikiPage Page { get; private set; } = null!;

    private WikiPageVersion() { }

    public static WikiPageVersion Create(WikiPageId pageId, int versionNumber,
        string contentJson, string contentHtml, Guid authorUserId,
        string? changeNote = null)
    {
        return new WikiPageVersion
        {
            Id = EntityId.New<WikiPageVersionId>(),
            WikiPageId = pageId,
            VersionNumber = versionNumber,
            ContentJson = contentJson,
            ContentHtml = contentHtml,
            AuthorUserId = authorUserId,
            ChangeNote = changeNote
        };
    }
}
