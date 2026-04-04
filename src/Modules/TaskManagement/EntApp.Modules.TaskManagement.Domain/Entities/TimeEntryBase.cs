using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Süre kaydı (time entry).</summary>
public sealed class TimeEntryBase : AuditableEntity<Guid>, ITenantEntity
{
    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Çalışma saati</summary>
    public decimal Hours { get; private set; }

    public DateTime WorkDate { get; private set; }

    public string? Description { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public TaskItemBase Task { get; private set; } = null!;

    private TimeEntryBase() { }

    public static TimeEntryBase Create(Guid taskId, Guid userId, decimal hours,
        DateTime workDate, string? description = null)
    {
        return new TimeEntryBase
        {
            Id = Guid.NewGuid(), TaskId = taskId, UserId = userId,
            Hours = hours, WorkDate = workDate.Date, Description = description
        };
    }
}
