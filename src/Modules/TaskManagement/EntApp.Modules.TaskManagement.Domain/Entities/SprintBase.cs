using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Sprint — Scrum metodolojisi için zaman kutusu.</summary>
public sealed class SprintBase : AuditableEntity<SprintId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    /// <summary>Sprint adı: "Sprint 1", "Sprint 2", ...</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Sprint hedefi.</summary>
    public string? Goal { get; private set; }

    public SprintStatus Status { get; private set; } = SprintStatus.Planning;

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    /// <summary>Takım kapasitesi (Story Points).</summary>
    public int? CapacityPoints { get; private set; }

    /// <summary>Sprint başladığında toplam planlanan SP.</summary>
    public int PlannedPoints { get; private set; }

    /// <summary>Sprint tamamlandığında tamamlanan SP.</summary>
    public int CompletedPoints { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProjectBase? Project { get; private set; }
    public ICollection<WorkItemBase> WorkItems { get; private set; } = [];

    private SprintBase() { }

    public static SprintBase Create(ProjectId projectId, string name,
        DateTime startDate, DateTime endDate,
        string? goal = null, int? capacityPoints = null)
    {
        return new SprintBase
        {
            Id = EntityId.New<SprintId>(),
            ProjectId = projectId,
            Name = name,
            Goal = goal,
            StartDate = startDate,
            EndDate = endDate,
            CapacityPoints = capacityPoints
        };
    }

    public void Update(string? name = null, string? goal = null,
        DateTime? startDate = null, DateTime? endDate = null,
        int? capacityPoints = null)
    {
        if (name is not null) Name = name;
        if (goal is not null) Goal = goal;
        if (startDate.HasValue) StartDate = startDate.Value;
        if (endDate.HasValue) EndDate = endDate.Value;
        if (capacityPoints.HasValue) CapacityPoints = capacityPoints.Value;
    }

    /// <summary>Sprint'i başlatır. Aynı projede başka aktif sprint olamaz (dışarıda kontrol edilmeli).</summary>
    public void Start()
    {
        if (Status != SprintStatus.Planning)
            throw new InvalidOperationException($"Sprint yalnızca Planning durumundayken başlatılabilir. Mevcut durum: {Status}");
        Status = SprintStatus.Active;
    }

    /// <summary>Sprint'i tamamlar.</summary>
    public void Complete(int completedPoints = 0)
    {
        if (Status != SprintStatus.Active)
            throw new InvalidOperationException($"Sprint yalnızca Active durumundayken tamamlanabilir. Mevcut durum: {Status}");
        CompletedPoints = completedPoints;
        Status = SprintStatus.Completed;
    }

    public void SetPlannedPoints(int points) => PlannedPoints = points;

    /// <summary>Sprint'i iptal eder.</summary>
    public void Cancel()
    {
        if (Status is SprintStatus.Completed or SprintStatus.Cancelled)
            throw new InvalidOperationException($"Tamamlanmış veya iptal edilmiş sprint tekrar iptal edilemez.");
        Status = SprintStatus.Cancelled;
    }
}
