using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Test adımı bazlı sonuç — her çalıştırmada her adımın sonucu ayrı kaydedilir.
/// </summary>
public sealed class TestStepResult : BaseEntity<Guid>
{
    public TestExecutionId TestExecutionId { get; private set; }
    public TestStepId TestStepId { get; private set; }

    /// <summary>Adım sonucu.</summary>
    public TestResult Result { get; private set; } = TestResult.NotRun;

    /// <summary>Gerçek sonuç açıklaması (Fail durumunda ne oldu).</summary>
    public string? ActualResult { get; private set; }

    /// <summary>Ek notlar.</summary>
    public string? Notes { get; private set; }

    // ── Navigation ──────────────────────────────────
    public TestExecution? TestExecution { get; private set; }
    public TestStep? TestStep { get; private set; }

    private TestStepResult() { }

    public static TestStepResult Create(
        TestExecutionId testExecutionId, TestStepId testStepId,
        TestResult result, string? actualResult = null, string? notes = null)
    {
        return new TestStepResult
        {
            Id = Guid.CreateVersion7(),
            TestExecutionId = testExecutionId,
            TestStepId = testStepId,
            Result = result,
            ActualResult = actualResult,
            Notes = notes
        };
    }
}
