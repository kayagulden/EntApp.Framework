using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>SLA tanımı — öncelik bazlı yanıt ve çözüm süreleri.</summary>
[DynamicEntity("SlaDefinition", MenuGroup = "Request Management")]
public sealed class SlaDefinition : AuditableEntity<SlaDefinitionId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Description { get; private set; }

    /// <summary>Öncelik bazlı yanıt süreleri (dakika). JSON: {"Low":480,"Medium":240,"High":120,"Critical":60,"Urgent":30}</summary>
    public string ResponseTimeJson { get; private set; } = "{}";

    /// <summary>Öncelik bazlı çözüm süreleri (dakika). JSON: {"Low":2880,"Medium":1440,"High":480,"Critical":240,"Urgent":120}</summary>
    public string ResolutionTimeJson { get; private set; } = "{}";

    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<RequestCategory> Categories { get; private set; } = [];

    private SlaDefinition() { }

    public static SlaDefinition Create(string name, string? description = null,
        string? responseTimeJson = null, string? resolutionTimeJson = null)
    {
        return new SlaDefinition
        {
            Id = EntityId.New<SlaDefinitionId>(),
            Name = name,
            Description = description,
            ResponseTimeJson = responseTimeJson ?? "{}",
            ResolutionTimeJson = resolutionTimeJson ?? "{}"
        };
    }

    public void Update(string name, string? description, string? responseTimeJson, string? resolutionTimeJson)
    {
        Name = name;
        Description = description;
        if (responseTimeJson is not null) ResponseTimeJson = responseTimeJson;
        if (resolutionTimeJson is not null) ResolutionTimeJson = resolutionTimeJson;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
