using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Proje.</summary>
[DynamicEntity("Project", MenuGroup = "Proje Yönetimi")]
public sealed class ProjectBase : AuditableEntity<ProjectId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 10, Searchable = true)]
    public string Key { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 2000)]
    public string? Description { get; private set; }

    public ProjectStatus Status { get; private set; } = ProjectStatus.Planning;
    public ProjectMethodology Methodology { get; private set; } = ProjectMethodology.Kanban;
    public ProjectCategory Category { get; private set; } = ProjectCategory.General;

    /// <summary>Proje tahmin gösterim modu (SP, T-Shirt, Saat).</summary>
    public EstimationDisplayMode EstimationMode { get; private set; } = EstimationDisplayMode.StoryPoints;

    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? TargetEndDate { get; private set; }

    /// <summary>Proje yöneticisi</summary>
    public Guid? ManagerUserId { get; private set; }

    /// <summary>Proje sahibi (PM veya sponsor).</summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>Opsiyonel portfolyo bağlantısı.</summary>
    public PortfolioId? PortfolioId { get; private set; }

    /// <summary>Otomatik görev numaralandırma sayacı</summary>
    public int WorkItemSequence { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public PortfolioBase? Portfolio { get; private set; }
    public ICollection<WorkItemBase> WorkItems { get; private set; } = [];
    public ICollection<ProjectDeliverable> Deliverables { get; private set; } = [];

    private ProjectBase() { }

    public static ProjectBase Create(string key, string name, string? description = null,
        DateTime? startDate = null, DateTime? endDate = null, DateTime? targetEndDate = null,
        Guid? managerUserId = null, Guid? ownerUserId = null,
        PortfolioId? portfolioId = null, ProjectMethodology methodology = ProjectMethodology.Kanban,
        ProjectCategory category = ProjectCategory.General)
    {
        return new ProjectBase
        {
            Id = EntityId.New<ProjectId>(),
            Key = key.ToUpperInvariant(),
            Name = name,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            TargetEndDate = targetEndDate,
            ManagerUserId = managerUserId,
            OwnerUserId = ownerUserId,
            PortfolioId = portfolioId,
            Methodology = methodology,
            Category = category
        };
    }

    public void Update(string? name = null, string? description = null,
        DateTime? startDate = null, DateTime? endDate = null, DateTime? targetEndDate = null,
        Guid? managerUserId = null, Guid? ownerUserId = null,
        PortfolioId? portfolioId = null, ProjectStatus? status = null,
        ProjectMethodology? methodology = null, ProjectCategory? category = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (startDate.HasValue) StartDate = startDate.Value;
        if (endDate.HasValue) EndDate = endDate.Value;
        if (targetEndDate.HasValue) TargetEndDate = targetEndDate.Value;
        if (managerUserId.HasValue) ManagerUserId = managerUserId.Value;
        if (ownerUserId.HasValue) OwnerUserId = ownerUserId.Value;
        if (portfolioId.HasValue) PortfolioId = portfolioId;
        if (status.HasValue) Status = status.Value;
        if (methodology.HasValue) Methodology = methodology.Value;
        if (category.HasValue) Category = category.Value;
    }

    public void Activate() => Status = ProjectStatus.Active;
    public void Complete() => Status = ProjectStatus.Completed;

    /// <summary>Yeni görev numarası üretir: KEY-1, KEY-2, ...</summary>
    public string NextWorkItemNumber()
    {
        WorkItemSequence++;
        return $"{Key}-{WorkItemSequence}";
    }

    /// <summary>Otomatik gereksinim numaralandırma sayacı.</summary>
    public int RequirementSequence { get; private set; }

    /// <summary>Yeni gereksinim numarası üretir: KEY-R1, KEY-R2, ...</summary>
    public string NextRequirementKey()
    {
        RequirementSequence++;
        return $"{Key}-R{RequirementSequence}";
    }
}
