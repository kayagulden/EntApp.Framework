using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Configuration.Domain.Entities;

/// <summary>
/// Entity başına UI override konfigürasyonu.
/// Admin veya tenant bazlı Dynamic UI metadata override'larını saklar.
/// Convention/attribute'dan gelen metadata'nın üzerine yazılır (3-tier fallback).
/// </summary>
public class DynamicUIConfig : AuditableEntity<Guid>
{
    /// <summary>Entity adı (DynamicEntity attribute name). Örn: "Country", "City"</summary>
    public string EntityName { get; private set; } = string.Empty;

    /// <summary>null ise global, değilse tenant'a özel override.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// JSON olarak saklanır — entity ve field seviyesinde UI override'ları.
    /// Schema: { title?, icon?, actions?, fields: { fieldName: { label?, order?, width?, showInList?, hidden?, searchable? } } }
    /// </summary>
    public string ConfigJson { get; private set; } = "{}";

    private DynamicUIConfig() { }

    public static DynamicUIConfig Create(
        string entityName,
        string configJson,
        Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity adı boş olamaz.", nameof(entityName));

        return new DynamicUIConfig
        {
            Id = Guid.NewGuid(),
            EntityName = entityName.Trim(),
            TenantId = tenantId,
            ConfigJson = configJson ?? "{}"
        };
    }

    public void UpdateConfig(string configJson)
    {
        ConfigJson = configJson ?? "{}";
    }
}
