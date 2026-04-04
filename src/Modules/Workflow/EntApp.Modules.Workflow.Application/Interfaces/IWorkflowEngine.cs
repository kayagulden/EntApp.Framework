using EntApp.Modules.Workflow.Domain.Entities;

namespace EntApp.Modules.Workflow.Application.Interfaces;

/// <summary>
/// Workflow motoru — akış yaşam döngüsü yönetimi.
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>Yeni workflow instance başlat.</summary>
    Task<WorkflowInstance> StartAsync(Guid definitionId, Guid? initiatorUserId = null,
        string? referenceType = null, string? referenceId = null,
        string? metadata = null, CancellationToken ct = default);

    /// <summary>Onay adımını onayla.</summary>
    Task<WorkflowInstance> ApproveAsync(Guid instanceId, Guid stepId, Guid userId,
        string? comment = null, CancellationToken ct = default);

    /// <summary>Onay adımını reddet.</summary>
    Task<WorkflowInstance> RejectAsync(Guid instanceId, Guid stepId, Guid userId,
        string? comment = null, CancellationToken ct = default);

    /// <summary>Adımı üst kademeye yönlendir.</summary>
    Task<WorkflowInstance> EscalateAsync(Guid instanceId, Guid stepId,
        Guid escalateToUserId, string? comment = null, CancellationToken ct = default);

    /// <summary>Workflow'u iptal et.</summary>
    Task<WorkflowInstance> CancelAsync(Guid instanceId, CancellationToken ct = default);

    /// <summary>Belirli kullanıcının onay bekleyen adımları.</summary>
    Task<IReadOnlyList<ApprovalStep>> GetPendingStepsAsync(Guid userId, CancellationToken ct = default);
}
