using EntApp.Modules.MyModule.Domain.Enums;
using EntApp.Modules.MyModule.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.MyModule.Domain.Entities;

/// <summary>
/// Örnek entity — kendi entity'lerinizi bu pattern'i kullanarak oluşturun.
/// İhtiyaç dışı alanları kaldırın, yeni alanlar ekleyin.
/// </summary>
[DynamicEntity("SampleEntity", MenuGroup = "MyModule")]
public sealed class SampleEntity : AuditableEntity<SampleEntityId>, ITenantEntity
{
    [DynamicField(Required = true, MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 2000)]
    public string? Description { get; set; }

    [DynamicField(FieldType = FieldType.Enum)]
    public SampleStatus Status { get; set; } = SampleStatus.Draft;

    public Guid TenantId { get; set; }

    // Factory method — entity oluşturma
    public static SampleEntity Create(string name, string? description = null)
    {
        return new SampleEntity
        {
            Id = EntityId.New<SampleEntityId>(),
            Name = name,
            Description = description,
            Status = SampleStatus.Draft
        };
    }
}
