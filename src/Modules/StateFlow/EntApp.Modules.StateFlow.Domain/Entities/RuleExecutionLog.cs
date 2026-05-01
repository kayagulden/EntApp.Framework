using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Entities;

namespace EntApp.Modules.StateFlow.Domain.Entities;

/// <summary>
/// Kural / Aksiyon çalışma kaydı.
/// OnEntryActions veya OnTransitionActions tetiklendiğinde her aksiyonun
/// sonucunu (başarı/hata, süre, tetikleyici veri) kaydeder.
/// 90 gün saklama politikası ile yönetilir.
/// </summary>
public sealed class RuleExecutionLog : AuditableEntity<RuleExecutionLogId>, ITenantEntity
{
    /// <summary>Hangi akış tanımından tetiklendi.</summary>
    public StateFlowDefinitionId FlowDefinitionId { get; private set; }

    /// <summary>Entity tipi: "Ticket", "WorkItem".</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Tetiklenen entity'nin ID'si.</summary>
    public Guid TargetEntityId { get; private set; }

    /// <summary>Kaynak: "OnEntry" veya "OnTransition".</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Kaynak state adı (transition için FromState, entry için state adı).</summary>
    public string StateName { get; private set; } = string.Empty;

    /// <summary>Tetikleyici trigger adı (transition için). OnEntry'de null olabilir.</summary>
    public string? TriggerName { get; private set; }

    /// <summary>Çalıştırılan aksiyon tipi: "SendNotification", "ChangeStatus" vb.</summary>
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>Aksiyonun parametre JSON'u (çalıştırma anındaki snapshot).</summary>
    public string ActionParamsJson { get; private set; } = "{}";

    /// <summary>Başarılı mı?</summary>
    public bool Success { get; private set; }

    /// <summary>Hata mesajı (başarısız ise).</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Çalışma süresi (ms).</summary>
    public int DurationMs { get; private set; }

    public Guid TenantId { get; set; }

    private RuleExecutionLog() { }

    public static RuleExecutionLog Create(
        StateFlowDefinitionId flowDefinitionId,
        string entityType, Guid entityId,
        string source, string stateName, string? triggerName,
        string actionType, string actionParamsJson,
        bool success, string? errorMessage, int durationMs)
    {
        return new RuleExecutionLog
        {
            Id = EntityId.New<RuleExecutionLogId>(),
            FlowDefinitionId = flowDefinitionId,
            EntityType = entityType,
            TargetEntityId = entityId,
            Source = source,
            StateName = stateName,
            TriggerName = triggerName,
            ActionType = actionType,
            ActionParamsJson = actionParamsJson,
            Success = success,
            ErrorMessage = errorMessage,
            DurationMs = durationMs,
            CreatedAt = DateTime.UtcNow
        };
    }
}
