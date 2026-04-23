using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Proje şablonu — önceden tanımlı board kolonları, milestone'lar ve iş kalemleri ile hızlı proje oluşturma.</summary>
public sealed class ProjectTemplate : AuditableEntity<ProjectTemplateId>, ITenantEntity
{
    /// <summary>Şablon adı: "Scrum Yazılım Projesi", "Altyapı Projesi" vb.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Şablon açıklaması.</summary>
    public string? Description { get; private set; }

    /// <summary>Emoji/icon kodu: "🚀", "⚙️" vb.</summary>
    public string? Icon { get; private set; }

    public ProjectMethodology Methodology { get; private set; } = ProjectMethodology.Kanban;
    public ProjectCategory Category { get; private set; } = ProjectCategory.General;
    public EstimationDisplayMode EstimationMode { get; private set; } = EstimationDisplayMode.StoryPoints;

    /// <summary>Framework ile gelen yerleşik şablon mu? (silinemez/düzenlenemez)</summary>
    public bool IsBuiltIn { get; private set; }

    /// <summary>Aktif mi — sadece aktif şablonlar listelenirsenur.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gösterim sırası.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Board kolon tanımları (JSON array).
    /// Format: [{"name":"Backlog","order":0,"mappedStatus":"Backlog","wipLimit":null}]</summary>
    public string BoardColumnsJson { get; private set; } = "[]";

    /// <summary>Milestone şablonları (JSON array — relative day offsets).
    /// Format: [{"name":"MVP Ready","dayOffset":30,"description":"..."}]</summary>
    public string? MilestonesJson { get; private set; }

    /// <summary>Başlangıç iş kalemi şablonları (JSON array).
    /// Format: [{"title":"Proje ortamını hazırla","type":"Task","priority":"High"}]</summary>
    public string? WorkItemsJson { get; private set; }

    public Guid TenantId { get; set; }

    private ProjectTemplate() { }

    public static ProjectTemplate Create(string name, string? description = null,
        string? icon = null,
        ProjectMethodology methodology = ProjectMethodology.Kanban,
        ProjectCategory category = ProjectCategory.General,
        EstimationDisplayMode estimationMode = EstimationDisplayMode.StoryPoints,
        bool isBuiltIn = false, int sortOrder = 0,
        string? boardColumnsJson = null,
        string? milestonesJson = null,
        string? workItemsJson = null)
    {
        return new ProjectTemplate
        {
            Id = EntityId.New<ProjectTemplateId>(),
            Name = name,
            Description = description,
            Icon = icon,
            Methodology = methodology,
            Category = category,
            EstimationMode = estimationMode,
            IsBuiltIn = isBuiltIn,
            SortOrder = sortOrder,
            BoardColumnsJson = boardColumnsJson ?? "[]",
            MilestonesJson = milestonesJson,
            WorkItemsJson = workItemsJson
        };
    }

    public void Update(string? name = null, string? description = null,
        string? icon = null,
        ProjectMethodology? methodology = null,
        ProjectCategory? category = null,
        EstimationDisplayMode? estimationMode = null,
        int? sortOrder = null, bool? isActive = null,
        string? boardColumnsJson = null,
        string? milestonesJson = null,
        string? workItemsJson = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (icon is not null) Icon = icon;
        if (methodology.HasValue) Methodology = methodology.Value;
        if (category.HasValue) Category = category.Value;
        if (estimationMode.HasValue) EstimationMode = estimationMode.Value;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
        if (boardColumnsJson is not null) BoardColumnsJson = boardColumnsJson;
        if (milestonesJson is not null) MilestonesJson = milestonesJson;
        if (workItemsJson is not null) WorkItemsJson = workItemsJson;
    }
}
