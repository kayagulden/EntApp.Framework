using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Test senaryosu — manuel test akışı tanımı.
/// Her senaryo bir veya daha fazla adımdan (TestStep) oluşur ve
/// opsiyonel olarak bir gereksinime (Requirement) bağlanır.
/// Numaralama proje bazlıdır: KEY-TC1, KEY-TC2, ...
/// </summary>
public sealed class TestScenario : AuditableEntity<TestScenarioId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    /// <summary>Proje bazlı senaryo numarası (KEY-TC1, KEY-TC2, ...).</summary>
    public string Key { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>Senaryo açıklaması (Markdown).</summary>
    public string? Description { get; private set; }

    /// <summary>Ön koşullar (Markdown).</summary>
    public string? Preconditions { get; private set; }

    public TestScenarioType Type { get; private set; } = TestScenarioType.Functional;
    public TestScenarioPriority Priority { get; private set; } = TestScenarioPriority.Medium;
    public TestScenarioStatus Status { get; private set; } = TestScenarioStatus.Draft;

    /// <summary>Hangi gereksinimden türetildi (nullable).</summary>
    public RequirementId? RequirementId { get; private set; }

    /// <summary>Tahmini çalıştırma süresi.</summary>
    public TimeSpan? EstimatedDuration { get; private set; }

    /// <summary>Etiketler (virgülle ayrılmış).</summary>
    public string? Tags { get; private set; }

    public int SortOrder { get; private set; }

    public Guid TenantId { get; set; }

    // ── Navigation ──────────────────────────────────
    public ProjectBase? Project { get; private set; }
    public Requirement? Requirement { get; private set; }
    public ICollection<TestStep> Steps { get; private set; } = [];

    private TestScenario() { }

    public static TestScenario Create(
        ProjectId projectId, string key, string title, TestScenarioType type,
        TestScenarioPriority priority = TestScenarioPriority.Medium,
        string? description = null, string? preconditions = null,
        RequirementId? requirementId = null,
        TimeSpan? estimatedDuration = null, string? tags = null,
        int sortOrder = 0)
    {
        return new TestScenario
        {
            Id = EntityId.New<TestScenarioId>(),
            ProjectId = projectId,
            Key = key,
            Title = title,
            Type = type,
            Priority = priority,
            Status = TestScenarioStatus.Draft,
            Description = description,
            Preconditions = preconditions,
            RequirementId = requirementId,
            EstimatedDuration = estimatedDuration,
            Tags = tags,
            SortOrder = sortOrder
        };
    }

    public void Update(string? title = null, string? description = null,
        TestScenarioType? type = null, TestScenarioPriority? priority = null,
        string? preconditions = null, RequirementId? requirementId = null,
        TimeSpan? estimatedDuration = null, string? tags = null)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (type.HasValue) Type = type.Value;
        if (priority.HasValue) Priority = priority.Value;
        if (preconditions is not null) Preconditions = preconditions;
        if (requirementId.HasValue) RequirementId = requirementId;
        if (estimatedDuration.HasValue) EstimatedDuration = estimatedDuration;
        if (tags is not null) Tags = tags;
    }

    public void UpdateStatus(TestScenarioStatus status) => Status = status;
    public void SetSortOrder(int order) => SortOrder = order;
    public void ClearRequirement() => RequirementId = null;
}
