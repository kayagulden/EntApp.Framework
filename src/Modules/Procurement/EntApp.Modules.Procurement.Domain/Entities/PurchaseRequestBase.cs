using EntApp.Modules.Procurement.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Procurement.Domain.Entities;

/// <summary>Satın alma talebi — onay akışına bağlanabilir.</summary>
[DynamicEntity("PurchaseRequest", MenuGroup = "Satın Alma")]
public sealed class PurchaseRequestBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string RequestNumber { get; private set; } = string.Empty;

    public Guid RequestedByUserId { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? Department { get; private set; }

    public PurchaseRequestStatus Status { get; private set; } = PurchaseRequestStatus.Draft;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 2000)]
    public string? Description { get; private set; }

    /// <summary>Talep kalemleri — JSON</summary>
    public string ItemsJson { get; private set; } = "[]";

    public decimal EstimatedTotal { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    public DateTime? RequiredByDate { get; private set; }

    /// <summary>Workflow instance (onay akışı)</summary>
    public Guid? WorkflowInstanceId { get; private set; }

    /// <summary>Onay sonrası oluşturulan PO</summary>
    public Guid? PurchaseOrderId { get; private set; }

    public Guid TenantId { get; set; }

    private PurchaseRequestBase() { }

    public static PurchaseRequestBase Create(string requestNumber, Guid requestedByUserId,
        string? department = null, string? description = null,
        string? itemsJson = null, decimal estimatedTotal = 0,
        string currency = "TRY", DateTime? requiredByDate = null)
    {
        return new PurchaseRequestBase
        {
            Id = Guid.NewGuid(), RequestNumber = requestNumber,
            RequestedByUserId = requestedByUserId, Department = department,
            Description = description, ItemsJson = itemsJson ?? "[]",
            EstimatedTotal = estimatedTotal, Currency = currency,
            RequiredByDate = requiredByDate
        };
    }

    public void Submit() => Status = PurchaseRequestStatus.Pending;
    public void Approve() => Status = PurchaseRequestStatus.Approved;
    public void Reject() => Status = PurchaseRequestStatus.Rejected;
    public void Cancel() => Status = PurchaseRequestStatus.Cancelled;
    public void MarkOrdered(Guid poId) { Status = PurchaseRequestStatus.Ordered; PurchaseOrderId = poId; }
    public void LinkWorkflow(Guid workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;
}
