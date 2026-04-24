using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Release notu — otomatik üretilir, kullanıcı düzenleyebilir.
/// Her release için en fazla bir not (1:1).
/// </summary>
public sealed class ReleaseNote : AuditableEntity<ReleaseNoteId>, ITenantEntity
{
    public ReleaseId ReleaseId { get; private set; }

    /// <summary>Markdown içerik — otomatik üretilir, düzenlenebilir.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Son otomatik üretim tarihi.</summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>Kullanıcı tarafından düzenlendi mi.</summary>
    public bool IsManuallyEdited { get; private set; }

    /// <summary>Yayınlanma tarihi.</summary>
    public DateTime? PublishedAt { get; private set; }

    public Guid TenantId { get; set; }

    // ── Navigation ──────────────────────────────────
    public Release? Release { get; private set; }

    private ReleaseNote() { }

    public static ReleaseNote Create(ReleaseId releaseId, string content)
    {
        return new ReleaseNote
        {
            Id = EntityId.New<ReleaseNoteId>(),
            ReleaseId = releaseId,
            Content = content,
            GeneratedAt = DateTime.UtcNow,
            IsManuallyEdited = false
        };
    }

    /// <summary>İçeriği otomatik üretim ile günceller.</summary>
    public void Regenerate(string content)
    {
        Content = content;
        GeneratedAt = DateTime.UtcNow;
        IsManuallyEdited = false;
    }

    /// <summary>Kullanıcı tarafından düzenleme.</summary>
    public void UpdateContent(string content)
    {
        Content = content;
        IsManuallyEdited = true;
    }

    /// <summary>Yayınla.</summary>
    public void Publish() => PublishedAt = DateTime.UtcNow;
}
