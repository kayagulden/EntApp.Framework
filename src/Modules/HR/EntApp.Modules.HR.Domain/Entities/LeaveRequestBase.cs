using EntApp.Modules.HR.Domain.Enums;
using EntApp.Modules.HR.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.HR.Domain.Entities;

/// <summary>İzin talebi — onay akışına bağlanabilir.</summary>
[DynamicEntity("LeaveRequest", MenuGroup = "İnsan Kaynakları")]
public sealed class LeaveRequestBase : AuditableEntity<LeaveRequestId>, ITenantEntity
{
    public EmployeeId EmployeeId { get; private set; }

    public LeaveType LeaveType { get; private set; } = LeaveType.Annual;
    public LeaveRequestStatus Status { get; private set; } = LeaveRequestStatus.Draft;

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    /// <summary>Toplam gün sayısı</summary>
    [DynamicField(FieldType = FieldType.Number)]
    public int TotalDays { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 1000)]
    public string? Reason { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 1000)]
    public string? ApproverComment { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>Workflow instance ID (onay akışı)</summary>
    public Guid? WorkflowInstanceId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public EmployeeBase Employee { get; private set; } = null!;

    private LeaveRequestBase() { }

    public static LeaveRequestBase Create(
        EmployeeId employeeId, LeaveType leaveType,
        DateTime startDate, DateTime endDate,
        string? reason = null)
    {
        var totalDays = (int)(endDate.Date - startDate.Date).TotalDays + 1;

        return new LeaveRequestBase
        {
            Id = EntityId.New<LeaveRequestId>(),
            EmployeeId = employeeId,
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = totalDays,
            Reason = reason
        };
    }

    public void Submit() => Status = LeaveRequestStatus.Pending;

    public void Approve(Guid approverUserId, string? comment = null)
    {
        Status = LeaveRequestStatus.Approved;
        ApprovedByUserId = approverUserId;
        ApprovedAt = DateTime.UtcNow;
        ApproverComment = comment;
    }

    public void Reject(Guid approverUserId, string? comment = null)
    {
        Status = LeaveRequestStatus.Rejected;
        ApprovedByUserId = approverUserId;
        ApprovedAt = DateTime.UtcNow;
        ApproverComment = comment;
    }

    public void Cancel() => Status = LeaveRequestStatus.Cancelled;

    public void LinkWorkflow(Guid workflowInstanceId)
        => WorkflowInstanceId = workflowInstanceId;
}
