using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Go/No-Go kontrol maddesi — checklist alt elemanı.
/// Her madde için ayrı onay/ret kaydı tutulur.
/// </summary>
public sealed class GoNoGoItem : BaseEntity<Guid>
{
    public GoNoGoChecklistId ChecklistId { get; private set; }

    public GoNoGoCategory Category { get; private set; }

    /// <summary>Kontrol maddesi başlığı.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Detay açıklama.</summary>
    public string? Description { get; private set; }

    public GoNoGoItemStatus Status { get; private set; } = GoNoGoItemStatus.Pending;

    /// <summary>Onaylayan/reddeden kişi.</summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>Onay/ret tarihi.</summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>Yorum.</summary>
    public string? Notes { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>Zorunlu mu? false ise skip edilebilir.</summary>
    public bool IsRequired { get; private set; } = true;

    // ── Navigation ──────────────────────────────────
    public GoNoGoChecklist? Checklist { get; private set; }

    private GoNoGoItem() { }

    public static GoNoGoItem Create(
        GoNoGoChecklistId checklistId, GoNoGoCategory category,
        string title, string? description = null,
        bool isRequired = true, int sortOrder = 0)
    {
        return new GoNoGoItem
        {
            Id = Guid.NewGuid(),
            ChecklistId = checklistId,
            Category = category,
            Title = title,
            Description = description,
            Status = GoNoGoItemStatus.Pending,
            IsRequired = isRequired,
            SortOrder = sortOrder
        };
    }

    /// <summary>Maddeyi onayla/reddet.</summary>
    public void Review(GoNoGoItemStatus status, string reviewedBy, string? notes = null)
    {
        Status = status;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        Notes = notes;
    }
}
