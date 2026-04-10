namespace EntApp.Modules.Workflow.Application.Interfaces;

/// <summary>
/// AI destekli workflow oluşturma ve tarif etme servisi.
/// Doğal dil ↔ Elsa Flowchart JSON dönüşümü yapar.
/// </summary>
public interface IWorkflowAiService
{
    /// <summary>
    /// Kullanıcının doğal dilde tarif ettiği workflow'u Elsa Flowchart JSON formatında üretir
    /// ve Elsa API üzerinden kaydeder.
    /// </summary>
    Task<WorkflowGenerationResult> GenerateFromPromptAsync(
        string prompt, string? name = null, CancellationToken ct = default);

    /// <summary>
    /// Mevcut bir Elsa workflow definition'ını doğal dilde (Türkçe) tarif eder.
    /// Kullanıcı bu tarifi değiştirip yeni bir workflow oluşturabilir.
    /// </summary>
    Task<WorkflowDescriptionResult> DescribeWorkflowAsync(
        string definitionId, CancellationToken ct = default);
}

/// <summary>Workflow oluşturma sonucu.</summary>
public sealed record WorkflowGenerationResult(
    string DefinitionId,
    string Name,
    string? Description,
    int ActivityCount,
    string Message);

/// <summary>Workflow tarif sonucu.</summary>
public sealed record WorkflowDescriptionResult(
    string DefinitionId,
    string Name,
    string Description,
    IReadOnlyList<ActivitySummary> Activities);

/// <summary>Bir activity'nin özeti.</summary>
public sealed record ActivitySummary(
    string Type,
    string DisplayName,
    string Description);
