using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Test senaryosu adımı — sıralı işlem ve beklenen sonuç.
/// </summary>
public sealed class TestStep : AuditableEntity<TestStepId>
{
    public TestScenarioId TestScenarioId { get; private set; }

    /// <summary>Adım sıra numarası (1, 2, 3...).</summary>
    public int StepNumber { get; private set; }

    /// <summary>Yapılacak işlem açıklaması.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Beklenen sonuç.</summary>
    public string ExpectedResult { get; private set; } = string.Empty;

    /// <summary>Test verisi (opsiyonel).</summary>
    public string? TestData { get; private set; }

    /// <summary>Ek notlar.</summary>
    public string? Notes { get; private set; }

    // ── Navigation ──────────────────────────────────
    public TestScenario? TestScenario { get; private set; }

    private TestStep() { }

    public static TestStep Create(
        TestScenarioId testScenarioId, int stepNumber,
        string action, string expectedResult,
        string? testData = null, string? notes = null)
    {
        return new TestStep
        {
            Id = EntityId.New<TestStepId>(),
            TestScenarioId = testScenarioId,
            StepNumber = stepNumber,
            Action = action,
            ExpectedResult = expectedResult,
            TestData = testData,
            Notes = notes
        };
    }

    public void Update(string? action = null, string? expectedResult = null,
        string? testData = null, string? notes = null, int? stepNumber = null)
    {
        if (action is not null) Action = action;
        if (expectedResult is not null) ExpectedResult = expectedResult;
        if (testData is not null) TestData = testData;
        if (notes is not null) Notes = notes;
        if (stepNumber.HasValue) StepNumber = stepNumber.Value;
    }
}
