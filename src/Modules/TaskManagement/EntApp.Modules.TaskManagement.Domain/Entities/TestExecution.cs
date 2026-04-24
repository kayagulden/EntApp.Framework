using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Test çalıştırma kaydı — bir plan+senaryo kombinasyonunun çalıştırılma sonucu.
/// Her çalıştırma adım bazlı sonuçları (TestStepResult) içerir.
/// </summary>
public sealed class TestExecution : AuditableEntity<TestExecutionId>, ITenantEntity
{
    /// <summary>Hangi plan+senaryo çifti (TestPlanScenario FK).</summary>
    public Guid TestPlanScenarioId { get; private set; }

    /// <summary>Çalıştıran kullanıcı.</summary>
    public string ExecutedBy { get; private set; } = string.Empty;

    /// <summary>Çalıştırma zamanı.</summary>
    public DateTime ExecutedAt { get; private set; }

    /// <summary>Genel sonuç.</summary>
    public TestResult Result { get; private set; } = TestResult.NotRun;

    /// <summary>Gerçek çalıştırma süresi.</summary>
    public TimeSpan? Duration { get; private set; }

    /// <summary>Notlar — başarısızlık nedeni, gözlemler.</summary>
    public string? Notes { get; private set; }

    /// <summary>Test ortamı bilgisi.</summary>
    public string? Environment { get; private set; }

    /// <summary>Fail durumunda oluşturulan Bug work item'ı.</summary>
    public WorkItemId? LinkedBugId { get; private set; }

    public Guid TenantId { get; set; }

    // ── Navigation ──────────────────────────────────
    public TestPlanScenario? TestPlanScenario { get; private set; }
    public ICollection<TestStepResult> StepResults { get; private set; } = [];

    private TestExecution() { }

    public static TestExecution Create(
        Guid testPlanScenarioId, string executedBy, TestResult result,
        TimeSpan? duration = null, string? notes = null,
        string? environment = null, WorkItemId? linkedBugId = null)
    {
        return new TestExecution
        {
            Id = EntityId.New<TestExecutionId>(),
            TestPlanScenarioId = testPlanScenarioId,
            ExecutedBy = executedBy,
            ExecutedAt = DateTime.UtcNow,
            Result = result,
            Duration = duration,
            Notes = notes,
            Environment = environment,
            LinkedBugId = linkedBugId
        };
    }

    public void LinkBug(WorkItemId bugId) => LinkedBugId = bugId;
}
