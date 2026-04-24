using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Release ↔ WorkItem M:N ilişkisi.
/// Hangi iş kalemi hangi release'e dahil edildi bilgisini tutar.
/// </summary>
public sealed class ReleaseItem : AuditableEntity<Guid>
{
    public ReleaseId ReleaseId { get; private set; }
    public WorkItemId WorkItemId { get; private set; }

    /// <summary>Release'e eklenme zamanı.</summary>
    public DateTime IncludedAt { get; private set; }

    /// <summary>Kim ekledi.</summary>
    public string IncludedBy { get; private set; } = string.Empty;

    /// <summary>Ekleme notu.</summary>
    public string? Notes { get; private set; }

    public int SortOrder { get; private set; }

    // ── Navigation ──────────────────────────────────
    public Release? Release { get; private set; }
    public WorkItemBase? WorkItem { get; private set; }

    private ReleaseItem() { }

    public static ReleaseItem Create(
        ReleaseId releaseId, WorkItemId workItemId,
        string includedBy, string? notes = null, int sortOrder = 0)
    {
        return new ReleaseItem
        {
            Id = Guid.NewGuid(),
            ReleaseId = releaseId,
            WorkItemId = workItemId,
            IncludedAt = DateTime.UtcNow,
            IncludedBy = includedBy,
            Notes = notes,
            SortOrder = sortOrder
        };
    }
}
