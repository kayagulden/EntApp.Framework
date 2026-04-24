using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Test planı — sprint veya milestone bazlı senaryo grubu.
/// Birden fazla senaryoyu bir araya getirip çalıştırma planlar.
/// Numaralama proje bazlıdır: KEY-TP1, KEY-TP2, ...
/// </summary>
public sealed class TestPlan : AuditableEntity<TestPlanId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    /// <summary>Proje bazlı plan numarası (KEY-TP1, KEY-TP2, ...).</summary>
    public string Key { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TestPlanStatus Status { get; private set; } = TestPlanStatus.Draft;

    /// <summary>Sprint bazlı plan (nullable).</summary>
    public SprintId? SprintId { get; private set; }

    /// <summary>Milestone/release bazlı plan (nullable).</summary>
    public MilestoneId? MilestoneId { get; private set; }

    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    /// <summary>Sorumlu tester userId.</summary>
    public string? AssignedTesterId { get; private set; }

    public Guid TenantId { get; set; }

    // ── Navigation ──────────────────────────────────
    public ProjectBase? Project { get; private set; }
    public ICollection<TestPlanScenario> Scenarios { get; private set; } = [];

    private TestPlan() { }

    public static TestPlan Create(
        ProjectId projectId, string key, string title,
        string? description = null,
        SprintId? sprintId = null, MilestoneId? milestoneId = null,
        DateOnly? startDate = null, DateOnly? endDate = null,
        string? assignedTesterId = null)
    {
        return new TestPlan
        {
            Id = EntityId.New<TestPlanId>(),
            ProjectId = projectId,
            Key = key,
            Title = title,
            Description = description,
            Status = TestPlanStatus.Draft,
            SprintId = sprintId,
            MilestoneId = milestoneId,
            StartDate = startDate,
            EndDate = endDate,
            AssignedTesterId = assignedTesterId
        };
    }

    public void Update(string? title = null, string? description = null,
        DateOnly? startDate = null, DateOnly? endDate = null,
        string? assignedTesterId = null)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (startDate.HasValue) StartDate = startDate;
        if (endDate.HasValue) EndDate = endDate;
        if (assignedTesterId is not null) AssignedTesterId = assignedTesterId;
    }

    public void UpdateStatus(TestPlanStatus status) => Status = status;
}
