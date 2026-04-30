using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Proje riski — olasılık × etki matrisi ile skorlanır.
/// RiskScore otomatik hesaplanır (Probability × Impact).
/// </summary>
public sealed class Risk : AuditableEntity<RiskId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>Risk açıklaması (Markdown).</summary>
    public string? Description { get; private set; }

    public RiskCategory Category { get; private set; } = RiskCategory.Technical;
    public RiskStatus Status { get; private set; } = RiskStatus.Open;

    /// <summary>Olasılık (1-5).</summary>
    public int Probability { get; private set; }

    /// <summary>Etki (1-5).</summary>
    public int Impact { get; private set; }

    /// <summary>Risk skoru (Probability × Impact, auto-calculated). Değer aralığı: 1-25.</summary>
    public int RiskScore { get; private set; }

    /// <summary>Genel azaltma stratejisi (Markdown).</summary>
    public string? MitigationPlan { get; private set; }

    /// <summary>Risk sahibi kullanıcı.</summary>
    public Guid? OwnerUserId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProjectBase? Project { get; private set; }
    public ICollection<MitigationAction> MitigationActions { get; private set; } = [];

    private Risk() { }

    public static Risk Create(ProjectId projectId, string title,
        RiskCategory category, int probability, int impact,
        string? description = null, string? mitigationPlan = null,
        Guid? ownerUserId = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(probability, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(probability, 5);
        ArgumentOutOfRangeException.ThrowIfLessThan(impact, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(impact, 5);

        return new Risk
        {
            Id = EntityId.New<RiskId>(),
            ProjectId = projectId,
            Title = title,
            Category = category,
            Probability = probability,
            Impact = impact,
            RiskScore = probability * impact,
            Description = description,
            MitigationPlan = mitigationPlan,
            OwnerUserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? title = null, string? description = null,
        RiskCategory? category = null, int? probability = null, int? impact = null,
        string? mitigationPlan = null, Guid? ownerUserId = null)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (category.HasValue) Category = category.Value;

        if (probability.HasValue || impact.HasValue)
        {
            var p = probability ?? Probability;
            var i = impact ?? Impact;
            ArgumentOutOfRangeException.ThrowIfLessThan(p, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(p, 5);
            ArgumentOutOfRangeException.ThrowIfLessThan(i, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(i, 5);
            Probability = p;
            Impact = i;
            RiskScore = p * i;
        }

        if (mitigationPlan is not null) MitigationPlan = mitigationPlan;
        if (ownerUserId.HasValue) OwnerUserId = ownerUserId;
    }

    public void UpdateStatus(RiskStatus status) => Status = status;
}
