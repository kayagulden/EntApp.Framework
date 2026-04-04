using EntApp.Modules.Workflow.Domain.Enums;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Workflow.Domain.Entities;

/// <summary>
/// Çalışan workflow örneği — bir tanımdan başlatılır.
/// </summary>
public sealed class WorkflowInstance : AuditableEntity<Guid>, ITenantEntity
{
    /// <summary>Bağlı tanım ID</summary>
    public Guid DefinitionId { get; private set; }

    /// <summary>Akış durumu</summary>
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Draft;

    /// <summary>Başlatan kullanıcı</summary>
    public Guid? InitiatorUserId { get; private set; }

    /// <summary>İlişkili kaynak tipi (ör: "LeaveRequest", "PurchaseOrder")</summary>
    public string? ReferenceType { get; private set; }

    /// <summary>İlişkili kaynak ID</summary>
    public string? ReferenceId { get; private set; }

    /// <summary>Mevcut adım sırası (Sequential modda)</summary>
    public int CurrentStepOrder { get; private set; }

    /// <summary>Başlangıç zamanı</summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>Bitiş zamanı</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Ek veri (JSON)</summary>
    public string? Metadata { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public WorkflowDefinition Definition { get; private set; } = null!;
    public ICollection<ApprovalStep> Steps { get; private set; } = [];

    private WorkflowInstance() { }

    public static WorkflowInstance Create(
        Guid definitionId,
        Guid? initiatorUserId = null,
        string? referenceType = null,
        string? referenceId = null,
        string? metadata = null)
    {
        return new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            DefinitionId = definitionId,
            Status = WorkflowStatus.Draft,
            InitiatorUserId = initiatorUserId,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Metadata = metadata,
            CurrentStepOrder = 0
        };
    }

    public void Start()
    {
        Status = WorkflowStatus.Active;
        StartedAt = DateTime.UtcNow;
        CurrentStepOrder = 1;
    }

    public void Complete()
    {
        Status = WorkflowStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = WorkflowStatus.Rejected;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = WorkflowStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public void TimeOut()
    {
        Status = WorkflowStatus.TimedOut;
        CompletedAt = DateTime.UtcNow;
    }

    public void AdvanceStep() => CurrentStepOrder++;
}
