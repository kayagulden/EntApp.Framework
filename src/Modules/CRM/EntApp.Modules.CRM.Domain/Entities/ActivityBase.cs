using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.CRM.Domain.Entities;

/// <summary>CRM aktivitesi — arama, e-posta, toplantı, not.</summary>
[DynamicEntity("Activity", MenuGroup = "CRM")]
public sealed class ActivityBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.Lookup)]
    [DynamicLookup(typeof(CustomerBase), DisplayField = "Name")]
    public Guid? CustomerId { get; private set; }

    [DynamicField(FieldType = FieldType.Lookup)]
    [DynamicLookup(typeof(OpportunityBase), DisplayField = "Title")]
    public Guid? OpportunityId { get; private set; }

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Subject { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 5000)]
    public string? Description { get; private set; }

    [DynamicField(FieldType = FieldType.Enum)]
    public ActivityType ActivityType { get; private set; } = ActivityType.Note;

    [DynamicField(FieldType = FieldType.Enum)]
    public ActivityStatus Status { get; private set; } = ActivityStatus.Planned;

    [DynamicField(FieldType = FieldType.Date)]
    public DateTime? DueDate { get; private set; }

    [DynamicField(FieldType = FieldType.Date)]
    public DateTime? CompletedAt { get; private set; }

    [DynamicField(FieldType = FieldType.Lookup)]
    public Guid? AssignedUserId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public CustomerBase? Customer { get; private set; }

    private ActivityBase() { }

    public static ActivityBase Create(
        string subject, ActivityType type,
        Guid? customerId = null, Guid? opportunityId = null,
        string? description = null, DateTime? dueDate = null,
        Guid? assignedUserId = null)
    {
        return new ActivityBase
        {
            Id = Guid.NewGuid(),
            Subject = subject, ActivityType = type,
            CustomerId = customerId, OpportunityId = opportunityId,
            Description = description, DueDate = dueDate,
            AssignedUserId = assignedUserId
        };
    }

    public void Complete()
    {
        Status = ActivityStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel() => Status = ActivityStatus.Cancelled;
}
