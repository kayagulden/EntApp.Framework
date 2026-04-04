using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;
using TaskStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.TaskStatus;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Görev / iş kalemi.</summary>
[DynamicEntity("TaskItem", MenuGroup = "Proje Yönetimi")]
public sealed class TaskItemBase : AuditableEntity<Guid>, ITenantEntity
{
    public Guid ProjectId { get; private set; }

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 20, Searchable = true)]
    public string TaskNumber { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 500, Searchable = true)]
    public string Title { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 5000)]
    public string? Description { get; private set; }

    public TaskStatusEnum Status { get; private set; } = TaskStatusEnum.Backlog;
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
    public TaskType Type { get; private set; } = TaskType.Task;

    /// <summary>Atanan kişi</summary>
    public Guid? AssigneeUserId { get; private set; }

    /// <summary>Raporlayan kişi</summary>
    public Guid? ReporterUserId { get; private set; }

    /// <summary>Üst görev (alt görev desteği)</summary>
    public Guid? ParentTaskId { get; private set; }

    public DateTime? DueDate { get; private set; }

    /// <summary>Tahmini süre (saat)</summary>
    public decimal EstimatedHours { get; private set; }

    /// <summary>Kanban sıralama</summary>
    public int SortOrder { get; private set; }

    /// <summary>Etiketler (virgülle ayrılmış)</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 500)]
    public string? Tags { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProjectBase Project { get; private set; } = null!;
    public TaskItemBase? ParentTask { get; private set; }
    public ICollection<TaskItemBase> SubTasks { get; private set; } = [];
    public ICollection<CommentBase> Comments { get; private set; } = [];
    public ICollection<TimeEntryBase> TimeEntries { get; private set; } = [];

    private TaskItemBase() { }

    public static TaskItemBase Create(Guid projectId, string taskNumber, string title,
        TaskType type = TaskType.Task, TaskPriority priority = TaskPriority.Medium,
        string? description = null, Guid? assigneeUserId = null,
        Guid? reporterUserId = null, Guid? parentTaskId = null,
        DateTime? dueDate = null, decimal estimatedHours = 0, string? tags = null)
    {
        return new TaskItemBase
        {
            Id = Guid.NewGuid(), ProjectId = projectId, TaskNumber = taskNumber,
            Title = title, Type = type, Priority = priority,
            Description = description, AssigneeUserId = assigneeUserId,
            ReporterUserId = reporterUserId, ParentTaskId = parentTaskId,
            DueDate = dueDate, EstimatedHours = estimatedHours, Tags = tags
        };
    }

    public void MoveTo(TaskStatusEnum status) => Status = status;
    public void AssignTo(Guid userId) => AssigneeUserId = userId;
    public void SetSortOrder(int order) => SortOrder = order;

    /// <summary>Harcanan toplam süre</summary>
    public decimal TotalLoggedHours => TimeEntries.Sum(t => t.Hours);
}
