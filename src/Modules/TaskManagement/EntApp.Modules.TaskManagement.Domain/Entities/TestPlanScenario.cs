using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Test planı ↔ senaryo M:N köprü tablosu.
/// Her kayıt bir plan içindeki bir senaryoyu temsil eder.
/// </summary>
public sealed class TestPlanScenario : BaseEntity<Guid>
{
    public TestPlanId TestPlanId { get; private set; }
    public TestScenarioId TestScenarioId { get; private set; }

    /// <summary>Bu plan içinde bu senaryoyu çalıştıracak tester.</summary>
    public string? AssignedTesterId { get; private set; }

    public int SortOrder { get; private set; }

    // ── Navigation ──────────────────────────────────
    public TestPlan? TestPlan { get; private set; }
    public TestScenario? TestScenario { get; private set; }
    public ICollection<TestExecution> Executions { get; private set; } = [];

    private TestPlanScenario() { }

    public static TestPlanScenario Create(
        TestPlanId testPlanId, TestScenarioId testScenarioId,
        string? assignedTesterId = null, int sortOrder = 0)
    {
        return new TestPlanScenario
        {
            Id = Guid.CreateVersion7(),
            TestPlanId = testPlanId,
            TestScenarioId = testScenarioId,
            AssignedTesterId = assignedTesterId,
            SortOrder = sortOrder
        };
    }
}
