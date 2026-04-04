using EntApp.Modules.Inventory.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Inventory.Domain.Entities;

/// <summary>Depo / lokasyon.</summary>
[DynamicEntity("Warehouse", MenuGroup = "Stok")]
public sealed class WarehouseBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string Code { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Address { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? City { get; private set; }

    public WarehouseStatus Status { get; private set; } = WarehouseStatus.Active;

    /// <summary>Sorumlu kişi</summary>
    public Guid? ManagerUserId { get; private set; }

    public Guid TenantId { get; set; }

    private WarehouseBase() { }

    public static WarehouseBase Create(string code, string name,
        string? address = null, string? city = null, Guid? managerUserId = null)
    {
        return new WarehouseBase
        {
            Id = Guid.NewGuid(), Code = code, Name = name,
            Address = address, City = city, ManagerUserId = managerUserId
        };
    }
}
