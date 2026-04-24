using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Gereksinim kaydı — hiyerarşik yapıda.
/// FeatureSpec tipi üst-seviye spec dokümanı yerine geçer (Description alanı zengin Markdown),
/// altındaki atomik gereksinimler izlenebilir kayıtlardır ve WorkItem'lara bağlanır.
/// Numaralama proje bazlıdır: KEY-R1, KEY-R2, ...
/// </summary>
public sealed class Requirement : AuditableEntity<RequirementId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    /// <summary>Üst gereksinim — FeatureSpec altında atomik gereksinimler.</summary>
    public RequirementId? ParentRequirementId { get; private set; }

    /// <summary>Proje bazlı gereksinim numarası (KEY-R1, KEY-R2, ...).</summary>
    public string Key { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>Markdown — FeatureSpec için spec dokümanı, atomik için kısa açıklama.</summary>
    public string? Description { get; private set; }

    /// <summary>Kabul kriterleri (Markdown).</summary>
    public string? AcceptanceCriteria { get; private set; }

    public RequirementType Type { get; private set; } = RequirementType.Functional;
    public RequirementPriority Priority { get; private set; } = RequirementPriority.Must;
    public RequirementStatus Status { get; private set; } = RequirementStatus.Draft;

    public int SortOrder { get; private set; }

    // ── Kaynak bağlantıları ─────────────────────────
    /// <summary>Hangi ticket'tan doğdu (nullable — ticket'sız da olabilir).</summary>
    public Guid? SourceTicketId { get; private set; }
    /// <summary>Kaynak ticket numarası referansı ("REQ-0042").</summary>
    public string? SourceTicketNumber { get; private set; }
    /// <summary>Figma/Miro/external tasarım linki.</summary>
    public string? ExternalDesignUrl { get; private set; }

    public Guid TenantId { get; set; }

    // ── Navigation ──────────────────────────────────
    public ProjectBase? Project { get; private set; }
    public Requirement? ParentRequirement { get; private set; }
    public ICollection<Requirement> Children { get; private set; } = [];
    public ICollection<WorkItemBase> WorkItems { get; private set; } = [];

    private Requirement() { }

    public static Requirement Create(
        ProjectId projectId, string key, string title, RequirementType type,
        RequirementPriority priority = RequirementPriority.Must,
        string? description = null, string? acceptanceCriteria = null,
        RequirementId? parentRequirementId = null,
        Guid? sourceTicketId = null, string? sourceTicketNumber = null,
        string? externalDesignUrl = null, int sortOrder = 0)
    {
        return new Requirement
        {
            Id = EntityId.New<RequirementId>(),
            ProjectId = projectId,
            Key = key,
            Title = title,
            Type = type,
            Priority = priority,
            Status = RequirementStatus.Draft,
            Description = description,
            AcceptanceCriteria = acceptanceCriteria,
            ParentRequirementId = parentRequirementId,
            SourceTicketId = sourceTicketId,
            SourceTicketNumber = sourceTicketNumber,
            ExternalDesignUrl = externalDesignUrl,
            SortOrder = sortOrder
        };
    }

    public void Update(string? title = null, string? description = null,
        RequirementType? type = null, RequirementPriority? priority = null,
        string? acceptanceCriteria = null, string? externalDesignUrl = null)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (type.HasValue) Type = type.Value;
        if (priority.HasValue) Priority = priority.Value;
        if (acceptanceCriteria is not null) AcceptanceCriteria = acceptanceCriteria;
        if (externalDesignUrl is not null) ExternalDesignUrl = externalDesignUrl;
    }

    public void UpdateStatus(RequirementStatus status) => Status = status;
    public void SetSortOrder(int order) => SortOrder = order;
}
