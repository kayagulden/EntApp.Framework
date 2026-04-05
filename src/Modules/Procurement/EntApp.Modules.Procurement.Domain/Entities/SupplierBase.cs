using EntApp.Modules.Procurement.Domain.Enums;
using EntApp.Modules.Procurement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Procurement.Domain.Entities;

/// <summary>Tedarikçi.</summary>
[DynamicEntity("Supplier", MenuGroup = "Satın Alma")]
public sealed class SupplierBase : AuditableEntity<SupplierId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string Code { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? Email { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? Phone { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Address { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? TaxNumber { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? ContactPerson { get; private set; }

    public SupplierRating Rating { get; private set; } = SupplierRating.Unrated;
    public bool IsActive { get; private set; } = true;

    /// <summary>Ödeme vadesi (gün)</summary>
    public int PaymentTermDays { get; private set; } = 30;

    public Guid TenantId { get; set; }

    private SupplierBase() { }

    public static SupplierBase Create(string code, string name,
        string? email = null, string? phone = null, string? address = null,
        string? taxNumber = null, string? contactPerson = null,
        int paymentTermDays = 30)
    {
        return new SupplierBase
        {
            Id = EntityId.New<SupplierId>(), Code = code, Name = name,
            Email = email, Phone = phone, Address = address,
            TaxNumber = taxNumber, ContactPerson = contactPerson,
            PaymentTermDays = paymentTermDays
        };
    }

    public void Rate(SupplierRating rating) => Rating = rating;
}
