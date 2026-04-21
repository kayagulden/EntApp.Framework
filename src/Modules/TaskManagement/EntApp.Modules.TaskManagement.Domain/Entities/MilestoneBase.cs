using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Milestone — proje zaman çizelgesinde bir kontrol noktası.</summary>
public sealed class MilestoneBase : AuditableEntity<MilestoneId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    /// <summary>Milestone adı: "Go-Live", "MVP Ready", "UAT Başlangıcı"...</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Açıklama / kabul kriterleri.</summary>
    public string? Description { get; private set; }

    /// <summary>Hedef tarih.</summary>
    public DateTime DueDate { get; private set; }

    /// <summary>Gerçekleşme tarihi (Reached olduğunda set edilir).</summary>
    public DateTime? CompletedDate { get; private set; }

    public MilestoneStatus Status { get; private set; } = MilestoneStatus.Pending;

    /// <summary>Sıralama (timeline görünümü için).</summary>
    public int SortOrder { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProjectBase? Project { get; private set; }
    public ICollection<WorkItemBase> WorkItems { get; private set; } = [];
    public ICollection<SprintBase> Sprints { get; private set; } = [];

    private MilestoneBase() { }

    public static MilestoneBase Create(ProjectId projectId, string name,
        DateTime dueDate, string? description = null, int sortOrder = 0)
    {
        return new MilestoneBase
        {
            Id = EntityId.New<MilestoneId>(),
            ProjectId = projectId,
            Name = name,
            DueDate = dueDate,
            Description = description,
            SortOrder = sortOrder
        };
    }

    public void Update(string? name = null, string? description = null,
        DateTime? dueDate = null, int? sortOrder = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (dueDate.HasValue) DueDate = dueDate.Value;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
    }

    /// <summary>Milestone'a ulaşıldığını işaretler.</summary>
    public void MarkReached(DateTime? completedDate = null)
    {
        Status = MilestoneStatus.Reached;
        CompletedDate = completedDate ?? DateTime.UtcNow;
    }

    /// <summary>Milestone kaçırıldı olarak işaretler.</summary>
    public void MarkMissed()
    {
        Status = MilestoneStatus.Missed;
    }

    /// <summary>Milestone'ı devam ediyor olarak işaretler.</summary>
    public void MarkInProgress()
    {
        Status = MilestoneStatus.InProgress;
    }

    /// <summary>Milestone'ı iptal eder.</summary>
    public void Cancel()
    {
        Status = MilestoneStatus.Cancelled;
    }

    /// <summary>Durumu doğrudan ayarlar (API'den gelen string için).</summary>
    public void SetStatus(MilestoneStatus status)
    {
        Status = status;
        if (status == MilestoneStatus.Reached && CompletedDate is null)
            CompletedDate = DateTime.UtcNow;
    }
}
