using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Risk azaltma aksiyonu — bir risk'e bağlı somut eylem planı.
/// </summary>
public sealed class MitigationAction : AuditableEntity<MitigationActionId>, ITenantEntity
{
    public RiskId RiskId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>Aksiyon açıklaması.</summary>
    public string? Description { get; private set; }

    public MitigationActionStatus Status { get; private set; } = MitigationActionStatus.Planned;

    /// <summary>Aksiyonu üstlenen kullanıcı.</summary>
    public Guid? AssigneeUserId { get; private set; }

    /// <summary>Hedef bitiş tarihi.</summary>
    public DateTime? DueDate { get; private set; }

    /// <summary>Tamamlanma tarihi.</summary>
    public DateTime? CompletedAt { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public Risk? Risk { get; private set; }

    private MitigationAction() { }

    public static MitigationAction Create(RiskId riskId, string title,
        string? description = null, Guid? assigneeUserId = null,
        DateTime? dueDate = null)
    {
        return new MitigationAction
        {
            Id = EntityId.New<MitigationActionId>(),
            RiskId = riskId,
            Title = title,
            Description = description,
            AssigneeUserId = assigneeUserId,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? title = null, string? description = null,
        MitigationActionStatus? status = null,
        Guid? assigneeUserId = null, DateTime? dueDate = null)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (status.HasValue)
        {
            Status = status.Value;
            if (status.Value == MitigationActionStatus.Completed)
                CompletedAt = DateTime.UtcNow;
        }
        if (assigneeUserId.HasValue) AssigneeUserId = assigneeUserId;
        if (dueDate.HasValue) DueDate = dueDate;
    }
}
