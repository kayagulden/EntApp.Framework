using EntApp.Modules.Workflow.Domain.Enums;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Workflow.Domain.Entities;

/// <summary>
/// Workflow tanımı — onay akışının şablonu.
/// Bir tanım birden fazla instance oluşturabilir.
/// </summary>
public sealed class WorkflowDefinition : AuditableEntity<Guid>, ITenantEntity
{
    /// <summary>Benzersiz akış adı (ör: "leave-approval", "purchase-order")</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Görüntülenecek başlık</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Açıklama</summary>
    public string? Description { get; private set; }

    /// <summary>Kategori (ör: "HR", "Finance", "IT")</summary>
    public string? Category { get; private set; }

    /// <summary>Onay tipi</summary>
    public ApprovalType ApprovalType { get; private set; } = ApprovalType.Sequential;

    /// <summary>Adım tanımları — JSON serileştirilmiş</summary>
    public string StepDefinitionsJson { get; private set; } = "[]";

    /// <summary>Zaman aşımı süresi (saat, null = sınırsız)</summary>
    public int? TimeoutHours { get; private set; }

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<WorkflowInstance> Instances { get; private set; } = [];

    private WorkflowDefinition() { }

    public static WorkflowDefinition Create(
        string name,
        string title,
        ApprovalType approvalType,
        string stepDefinitionsJson,
        string? description = null,
        string? category = null,
        int? timeoutHours = null)
    {
        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Title = title,
            ApprovalType = approvalType,
            StepDefinitionsJson = stepDefinitionsJson,
            Description = description,
            Category = category,
            TimeoutHours = timeoutHours
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
