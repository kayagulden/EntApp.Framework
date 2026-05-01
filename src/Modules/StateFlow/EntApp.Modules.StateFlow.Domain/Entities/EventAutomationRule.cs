using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.StateFlow.Domain.Entities;

/// <summary>
/// StateFlow'dan bağımsız, sistem olaylarına tepki veren otomasyon kuralı.
/// Örn: SLA aşımında bildirim, Ticket idle timeout'ta yöneticiye uyarı vb.
/// </summary>
public sealed class EventAutomationRule : AuditableEntity<EventAutomationRuleId>, ITenantEntity
{
    /// <summary>Kural adı. Örn: "SLA Yanıt Süresi Aşımı Bildirimi"</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Açıklama.</summary>
    public string? Description { get; private set; }

    /// <summary>Tetikleyici tipi. Örn: SLAResponseBreached, TicketIdleTimeout</summary>
    public string TriggerType { get; private set; } = string.Empty;

    /// <summary>Tetikleyici koşulları (JSON). Örn: {"priority":"Critical","slaType":"Response"}</summary>
    public string TriggerConditions { get; private set; } = "{}";

    /// <summary>Çalıştırılacak aksiyon tipi. Örn: SendNotification, AddComment</summary>
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>Aksiyon parametreleri (JSON). Örn: {"channel":"Email","template":"sla_breach"}</summary>
    public string ActionParams { get; private set; } = "{}";

    /// <summary>Hangi entity tipine uygulanır. Null = tümü.</summary>
    public string? EntityType { get; private set; }

    /// <summary>Kural aktif mi?</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Çalışma önceliği (düşük = önce).</summary>
    public int Priority { get; private set; }

    /// <summary>Sıralama.</summary>
    public int SortOrder { get; private set; }

    public Guid TenantId { get; set; }

    private EventAutomationRule() { }

    public static EventAutomationRule Create(
        string name, string triggerType, string actionType,
        string? description = null, string? triggerConditions = null,
        string? actionParams = null, string? entityType = null,
        int priority = 0, int sortOrder = 0)
    {
        return new EventAutomationRule
        {
            Id = EntityId.New<EventAutomationRuleId>(),
            Name = name,
            Description = description,
            TriggerType = triggerType,
            TriggerConditions = triggerConditions ?? "{}",
            ActionType = actionType,
            ActionParams = actionParams ?? "{}",
            EntityType = entityType,
            Priority = priority,
            SortOrder = sortOrder,
        };
    }

    public void Update(
        string name, string triggerType, string actionType,
        string? description, string? triggerConditions,
        string? actionParams, string? entityType,
        int priority, int sortOrder)
    {
        Name = name;
        Description = description;
        TriggerType = triggerType;
        TriggerConditions = triggerConditions ?? "{}";
        ActionType = actionType;
        ActionParams = actionParams ?? "{}";
        EntityType = entityType;
        Priority = priority;
        SortOrder = sortOrder;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
    public void Toggle() => IsEnabled = !IsEnabled;
}
