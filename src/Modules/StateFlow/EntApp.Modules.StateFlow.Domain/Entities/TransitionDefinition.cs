using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.StateFlow.Domain.Entities;

/// <summary>
/// Geçiş Tanımı — iki state arasındaki izin verilen geçiş.
/// FromStateName → ToStateName yönünde, TriggerName tetikleyicisi ile çalışır.
/// </summary>
public sealed class TransitionDefinition : AuditableEntity<TransitionDefinitionId>
{
    /// <summary>Ait olduğu akış tanımı.</summary>
    public StateFlowDefinitionId FlowDefinitionId { get; private set; }

    /// <summary>Kaynak state adı: "InProgress".</summary>
    public string FromStateName { get; private set; } = string.Empty;

    /// <summary>Hedef state adı: "Done".</summary>
    public string ToStateName { get; private set; } = string.Empty;

    /// <summary>Tetikleyici adı: "Resolve", "Cancel", "Assign".</summary>
    public string TriggerName { get; private set; } = string.Empty;

    /// <summary>Kullanıcıya gösterilen etiket: "Çöz", "İptal Et".</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Geçiş için gerekli rol: "Agent", "Manager". Null ise herkes yapabilir.</summary>
    public string? RequiredRole { get; private set; }

    /// <summary>Koşul ifadesi (opsiyonel, gelecek kullanım için).</summary>
    public string? GuardExpression { get; private set; }

    /// <summary>Sıralama (butonların gösterim sırası).</summary>
    public int SortOrder { get; private set; }

    // Navigation
    public StateFlowDefinition FlowDefinition { get; private set; } = null!;

    private TransitionDefinition() { }

    public static TransitionDefinition Create(
        StateFlowDefinitionId flowDefinitionId,
        string fromStateName, string toStateName,
        string triggerName, string label,
        string? requiredRole = null, string? guardExpression = null,
        int sortOrder = 0)
    {
        return new TransitionDefinition
        {
            Id = EntityId.New<TransitionDefinitionId>(),
            FlowDefinitionId = flowDefinitionId,
            FromStateName = fromStateName,
            ToStateName = toStateName,
            TriggerName = triggerName,
            Label = label,
            RequiredRole = requiredRole,
            GuardExpression = guardExpression,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string fromStateName, string toStateName,
        string triggerName, string label,
        string? requiredRole, string? guardExpression,
        int sortOrder)
    {
        FromStateName = fromStateName;
        ToStateName = toStateName;
        TriggerName = triggerName;
        Label = label;
        RequiredRole = requiredRole;
        GuardExpression = guardExpression;
        SortOrder = sortOrder;
    }

    /// <summary>Kopyalama (yeni versiyon veya şablon klonlama için).</summary>
    public TransitionDefinition Clone(StateFlowDefinitionId newFlowDefinitionId)
    {
        return new TransitionDefinition
        {
            Id = EntityId.New<TransitionDefinitionId>(),
            FlowDefinitionId = newFlowDefinitionId,
            FromStateName = FromStateName,
            ToStateName = ToStateName,
            TriggerName = TriggerName,
            Label = Label,
            RequiredRole = RequiredRole,
            GuardExpression = GuardExpression,
            SortOrder = SortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }
}
