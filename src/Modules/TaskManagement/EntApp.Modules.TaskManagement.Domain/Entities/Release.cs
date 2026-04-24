using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Release — proje bazlı sürüm yönetimi.
/// Numaralama: KEY-REL1, KEY-REL2, ...
/// </summary>
public sealed class Release : AuditableEntity<ReleaseId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    /// <summary>Proje bazlı release numarası (KEY-REL1, KEY-REL2, ...).</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Serbest format versiyon numarası (v1.0.0, 2026.04.1, vb.).</summary>
    public string Version { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>Markdown açıklama.</summary>
    public string? Description { get; private set; }

    public ReleaseStatus Status { get; private set; } = ReleaseStatus.Planning;
    public ReleaseType Type { get; private set; } = ReleaseType.Minor;

    /// <summary>Hangi sprint'in çıktısı (nullable).</summary>
    public SprintId? SprintId { get; private set; }

    /// <summary>Hangi milestone'a bağlı (nullable).</summary>
    public MilestoneId? MilestoneId { get; private set; }

    /// <summary>Planlanan release tarihi.</summary>
    public DateOnly? PlannedDate { get; private set; }

    /// <summary>Gerçekleşen release tarihi.</summary>
    public DateOnly? ActualDate { get; private set; }

    /// <summary>Kod dondurma tarihi.</summary>
    public DateOnly? CodeFreezeDate { get; private set; }

    /// <summary>Release sorumlusu userId.</summary>
    public string? ReleaseManagerId { get; private set; }

    /// <summary>Hedef ortam (Production, Staging, vb.).</summary>
    public string? TargetEnvironment { get; private set; }

    /// <summary>Virgülle ayrılmış etiketler.</summary>
    public string? Tags { get; private set; }

    public int SortOrder { get; private set; }

    public Guid TenantId { get; set; }

    // ── Navigation ──────────────────────────────────
    public ProjectBase? Project { get; private set; }
    public ICollection<ReleaseItem> Items { get; private set; } = [];
    public GoNoGoChecklist? GoNoGoChecklist { get; private set; }
    public ReleaseNote? ReleaseNote { get; private set; }

    private Release() { }

    public static Release Create(
        ProjectId projectId, string key, string version, string title,
        ReleaseType type = ReleaseType.Minor,
        string? description = null,
        SprintId? sprintId = null, MilestoneId? milestoneId = null,
        DateOnly? plannedDate = null, DateOnly? codeFreezeDate = null,
        string? releaseManagerId = null, string? targetEnvironment = null,
        string? tags = null, int sortOrder = 0)
    {
        return new Release
        {
            Id = EntityId.New<ReleaseId>(),
            ProjectId = projectId,
            Key = key,
            Version = version,
            Title = title,
            Type = type,
            Description = description,
            Status = ReleaseStatus.Planning,
            SprintId = sprintId,
            MilestoneId = milestoneId,
            PlannedDate = plannedDate,
            CodeFreezeDate = codeFreezeDate,
            ReleaseManagerId = releaseManagerId,
            TargetEnvironment = targetEnvironment,
            Tags = tags,
            SortOrder = sortOrder
        };
    }

    public void Update(string? version = null, string? title = null, string? description = null,
        ReleaseType? type = null,
        DateOnly? plannedDate = null, DateOnly? actualDate = null, DateOnly? codeFreezeDate = null,
        string? releaseManagerId = null, string? targetEnvironment = null,
        string? tags = null, int? sortOrder = null)
    {
        if (version is not null) Version = version;
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (type.HasValue) Type = type.Value;
        if (plannedDate.HasValue) PlannedDate = plannedDate;
        if (actualDate.HasValue) ActualDate = actualDate;
        if (codeFreezeDate.HasValue) CodeFreezeDate = codeFreezeDate;
        if (releaseManagerId is not null) ReleaseManagerId = releaseManagerId;
        if (targetEnvironment is not null) TargetEnvironment = targetEnvironment;
        if (tags is not null) Tags = tags;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
    }

    public void UpdateStatus(ReleaseStatus status)
    {
        Status = status;
        if (status == ReleaseStatus.Deployed && !ActualDate.HasValue)
            ActualDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
