using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Configuration Item (CI) — CMDB'nin temel yapı taşı.
/// Tüm IT varlıkları (Application, Server, Database vb.) bu sınıftan türer.
/// TPT stratejisi: configuration_items tablosu + türemiş tablolar.
/// </summary>
public abstract class ConfigurationItemBase : AuditableEntity<ConfigurationItemId>, ITenantEntity
{
    public string Name { get; protected set; } = string.Empty;
    public string Code { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }

    /// <summary>CI durumu — tüm CI tipleri paylaşır.</summary>
    public CIStatus Status { get; protected set; } = CIStatus.Planned;

    /// <summary>Kritiklik seviyesi — SLA, etki analizi vb. için.</summary>
    public CICriticality Criticality { get; protected set; } = CICriticality.Medium;

    /// <summary>İş sahibi / sorumlu.</summary>
    public Guid? OwnerUserId { get; protected set; }

    public Guid TenantId { get; set; }

    // Navigation — projelerle ilişki
    public ICollection<ProjectDeliverable> ProjectDeliverables { get; protected set; } = [];

    public void Retire() => Status = CIStatus.Retired;
    public void Deprecate() => Status = CIStatus.Deprecated;
    public void Activate() => Status = CIStatus.Active;

    protected ConfigurationItemBase()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Ortak CI alanlarını günceller.</summary>
    protected void UpdateBase(string? name = null, string? description = null,
        CIStatus? status = null, CICriticality? criticality = null,
        Guid? ownerUserId = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (status.HasValue) Status = status.Value;
        if (criticality.HasValue) Criticality = criticality.Value;
        if (ownerUserId.HasValue) OwnerUserId = ownerUserId.Value;
    }
}
