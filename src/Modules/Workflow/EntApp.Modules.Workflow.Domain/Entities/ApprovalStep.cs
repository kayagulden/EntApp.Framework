using EntApp.Modules.Workflow.Domain.Enums;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Workflow.Domain.Entities;

/// <summary>
/// Tekil onay adımı — bir instance'ın parçası.
/// </summary>
public sealed class ApprovalStep : AuditableEntity<Guid>, ITenantEntity
{
    /// <summary>Bağlı instance ID</summary>
    public Guid InstanceId { get; private set; }

    /// <summary>Adım sırası (1, 2, 3...)</summary>
    public int StepOrder { get; private set; }

    /// <summary>Adım adı</summary>
    public string StepName { get; private set; } = string.Empty;

    /// <summary>Onaylayıcı kullanıcı ID</summary>
    public Guid? AssignedUserId { get; private set; }

    /// <summary>Onaylayıcı rol adı (kullanıcı yerine rol bazlı)</summary>
    public string? AssignedRole { get; private set; }

    /// <summary>Adım durumu</summary>
    public StepStatus Status { get; private set; } = StepStatus.Pending;

    /// <summary>Onaylayan/reddeden kullanıcı</summary>
    public Guid? ActionByUserId { get; private set; }

    /// <summary>İşlem zamanı</summary>
    public DateTime? ActionAt { get; private set; }

    /// <summary>Onaylayan yorumu</summary>
    public string? Comment { get; private set; }

    /// <summary>Zaman aşımı tarihi</summary>
    public DateTime? DueDate { get; private set; }

    /// <summary>Eskalasyon hedefi (üst kademe kullanıcı ID)</summary>
    public Guid? EscalationUserId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public WorkflowInstance Instance { get; private set; } = null!;

    private ApprovalStep() { }

    public static ApprovalStep Create(
        Guid instanceId,
        int stepOrder,
        string stepName,
        Guid? assignedUserId = null,
        string? assignedRole = null,
        DateTime? dueDate = null,
        Guid? escalationUserId = null)
    {
        return new ApprovalStep
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            StepOrder = stepOrder,
            StepName = stepName,
            AssignedUserId = assignedUserId,
            AssignedRole = assignedRole,
            DueDate = dueDate,
            EscalationUserId = escalationUserId
        };
    }

    public void Approve(Guid userId, string? comment = null)
    {
        Status = StepStatus.Approved;
        ActionByUserId = userId;
        ActionAt = DateTime.UtcNow;
        Comment = comment;
    }

    public void Reject(Guid userId, string? comment = null)
    {
        Status = StepStatus.Rejected;
        ActionByUserId = userId;
        ActionAt = DateTime.UtcNow;
        Comment = comment;
    }

    public void Skip(string? comment = null)
    {
        Status = StepStatus.Skipped;
        ActionAt = DateTime.UtcNow;
        Comment = comment;
    }

    public void Escalate(Guid escalateToUserId, string? comment = null)
    {
        Status = StepStatus.Escalated;
        EscalationUserId = escalateToUserId;
        ActionAt = DateTime.UtcNow;
        Comment = comment;
    }
}
