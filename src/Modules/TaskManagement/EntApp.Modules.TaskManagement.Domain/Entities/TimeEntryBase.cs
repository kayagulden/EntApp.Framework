using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Süre kaydı (time entry).</summary>
public sealed class TimeEntryBase : AuditableEntity<TimeEntryId>, ITenantEntity
{
    public WorkItemId TaskId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Çalışma saati</summary>
    public decimal Hours { get; private set; }

    public DateTime WorkDate { get; private set; }

    public string? Description { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public WorkItemBase Task { get; private set; } = null!;

    private TimeEntryBase() { }

    public static TimeEntryBase Create(WorkItemId taskId, Guid userId, decimal hours,
        DateTime workDate, string? description = null)
    {
        return new TimeEntryBase
        {
            Id = EntityId.New<TimeEntryId>(), TaskId = taskId, UserId = userId,
            Hours = hours, WorkDate = workDate.Date, Description = description
        };
    }
}
