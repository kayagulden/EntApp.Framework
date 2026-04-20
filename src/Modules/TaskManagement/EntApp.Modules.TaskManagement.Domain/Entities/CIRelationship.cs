using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// CI-CI ilişkisi — CMDB graf yapısının temel yapı taşı.
/// İki CI arasındaki yönlü ilişkiyi tanımlar (runs_on, depends_on vb.).
/// </summary>
public sealed class CIRelationship : AuditableEntity<CIRelationshipId>, ITenantEntity
{
    /// <summary>İlişkinin kaynağı (başlangıç noktası).</summary>
    public ConfigurationItemId SourceCIId { get; private set; }

    /// <summary>İlişkinin hedefi.</summary>
    public ConfigurationItemId TargetCIId { get; private set; }

    /// <summary>İlişki tipi — ITIL CMDB relationship types.</summary>
    public CIRelationType RelationType { get; private set; }

    /// <summary>Opsiyonel açıklama notu.</summary>
    public string? Notes { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ConfigurationItemBase? SourceCI { get; private set; }
    public ConfigurationItemBase? TargetCI { get; private set; }

    private CIRelationship() { }

    public static CIRelationship Create(ConfigurationItemId sourceCIId,
        ConfigurationItemId targetCIId, CIRelationType relationType, string? notes = null)
    {
        return new CIRelationship
        {
            Id = EntityId.New<CIRelationshipId>(),
            SourceCIId = sourceCIId,
            TargetCIId = targetCIId,
            RelationType = relationType,
            Notes = notes
        };
    }

    public void UpdateNotes(string? notes) => Notes = notes;
}
