using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.StateFlow.Domain.Entities;

/// <summary>
/// Durum Tanımı — bir akıştaki tek bir state node'u.
/// Semantic flag'ler (IsInitial, IsTerminal, IsPaused) sayesinde
/// kod, state adına değil anlam bayrağına göre davranış değiştirir.
/// </summary>
public sealed class StateDefinition : AuditableEntity<StateDefinitionId>
{
    /// <summary>Ait olduğu akış tanımı.</summary>
    public StateFlowDefinitionId FlowDefinitionId { get; private set; }

    /// <summary>State adı (PascalCase): "New", "InProgress", "Done".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Kullanıcıya gösterilen etiket: "Yeni", "İşlemde", "Tamamlandı".</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>Renk kodu: "#3b82f6".</summary>
    public string Color { get; private set; } = "#6b7280";

    /// <summary>İkon adı: "circle", "clock", "check".</summary>
    public string? Icon { get; private set; }

    // ── Semantic Flags ──────────────────────────────────────
    /// <summary>Başlangıç state'i mi? Yeni entity bu state'le oluşturulur.</summary>
    public bool IsInitial { get; private set; }

    /// <summary>Bitiş state'i mi? SLA durdur, raporlarda "kapalı" say.</summary>
    public bool IsTerminal { get; private set; }

    /// <summary>Beklemede mi? SLA saatini duraklat.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>Kategori: "Active", "Waiting", "Closed" — dashboard ve filtreleme.</summary>
    public string Category { get; private set; } = "Active";

    // ── Designer Pozisyon ───────────────────────────────────
    /// <summary>Designer canvas X koordinatı.</summary>
    public double PositionX { get; private set; }

    /// <summary>Designer canvas Y koordinatı.</summary>
    public double PositionY { get; private set; }

    /// <summary>Sıralama (dropdown'larda, listede).</summary>
    public int SortOrder { get; private set; }

    /// <summary>State'e girildiğinde tetiklenecek aksiyonlar (JSON). Örnek: [{"type":"notification","to":"assignee"}]</summary>
    public string? OnEntryActions { get; private set; }

    // Navigation
    public StateFlowDefinition FlowDefinition { get; private set; } = null!;

    private StateDefinition() { }

    public static StateDefinition Create(
        StateFlowDefinitionId flowDefinitionId,
        string name, string label,
        string color = "#6b7280", string? icon = null,
        bool isInitial = false, bool isTerminal = false, bool isPaused = false,
        string category = "Active",
        double positionX = 0, double positionY = 0,
        int sortOrder = 0, string? onEntryActions = null)
    {
        return new StateDefinition
        {
            Id = EntityId.New<StateDefinitionId>(),
            FlowDefinitionId = flowDefinitionId,
            Name = name,
            Label = label,
            Color = color,
            Icon = icon,
            IsInitial = isInitial,
            IsTerminal = isTerminal,
            IsPaused = isPaused,
            Category = category,
            PositionX = positionX,
            PositionY = positionY,
            SortOrder = sortOrder,
            OnEntryActions = onEntryActions,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string label, string color, string? icon,
        bool isInitial, bool isTerminal, bool isPaused,
        string category, double positionX, double positionY,
        int sortOrder, string? onEntryActions)
    {
        Label = label;
        Color = color;
        Icon = icon;
        IsInitial = isInitial;
        IsTerminal = isTerminal;
        IsPaused = isPaused;
        Category = category;
        PositionX = positionX;
        PositionY = positionY;
        SortOrder = sortOrder;
        OnEntryActions = onEntryActions;
    }

    /// <summary>Kopyalama (yeni versiyon veya şablon klonlama için).</summary>
    public StateDefinition Clone(StateFlowDefinitionId newFlowDefinitionId)
    {
        return new StateDefinition
        {
            Id = EntityId.New<StateDefinitionId>(),
            FlowDefinitionId = newFlowDefinitionId,
            Name = Name,
            Label = Label,
            Color = Color,
            Icon = Icon,
            IsInitial = IsInitial,
            IsTerminal = IsTerminal,
            IsPaused = IsPaused,
            Category = Category,
            PositionX = PositionX,
            PositionY = PositionY,
            SortOrder = SortOrder,
            OnEntryActions = OnEntryActions,
            CreatedAt = DateTime.UtcNow
        };
    }
}
