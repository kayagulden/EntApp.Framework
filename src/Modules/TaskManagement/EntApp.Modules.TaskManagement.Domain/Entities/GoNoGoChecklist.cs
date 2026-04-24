using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Go/No-Go checklist — release deploy kararı için kontrol listesi.
/// Her release için en fazla bir checklist oluşturulur (1:1).
/// </summary>
public sealed class GoNoGoChecklist : AuditableEntity<GoNoGoChecklistId>, ITenantEntity
{
    public ReleaseId ReleaseId { get; private set; }

    public GoNoGoStatus Status { get; private set; } = GoNoGoStatus.Pending;

    /// <summary>Karar tarihi.</summary>
    public DateTime? DecisionAt { get; private set; }

    /// <summary>Kararı veren kişi.</summary>
    public string? DecisionBy { get; private set; }

    /// <summary>Karar notu.</summary>
    public string? DecisionNotes { get; private set; }

    public Guid TenantId { get; set; }

    // ── Navigation ──────────────────────────────────
    public Release? Release { get; private set; }
    public ICollection<GoNoGoItem> Items { get; private set; } = [];

    private GoNoGoChecklist() { }

    public static GoNoGoChecklist Create(ReleaseId releaseId)
    {
        return new GoNoGoChecklist
        {
            Id = EntityId.New<GoNoGoChecklistId>(),
            ReleaseId = releaseId,
            Status = GoNoGoStatus.Pending
        };
    }

    /// <summary>Genel karar ver (Approved / Rejected).</summary>
    public void Decide(GoNoGoStatus status, string decidedBy, string? notes = null)
    {
        Status = status;
        DecisionAt = DateTime.UtcNow;
        DecisionBy = decidedBy;
        DecisionNotes = notes;
    }

    /// <summary>Durumu InProgress yap.</summary>
    public void MarkInProgress()
    {
        if (Status == GoNoGoStatus.Pending)
            Status = GoNoGoStatus.InProgress;
    }
}
