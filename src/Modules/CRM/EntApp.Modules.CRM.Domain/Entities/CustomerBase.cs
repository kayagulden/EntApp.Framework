using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.CRM.Domain.Entities;

/// <summary>Müşteri — CRM'in temel entity'si.</summary>
[DynamicEntity("Customer", MenuGroup = "CRM")]
public sealed class CustomerBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, MaxLength = 100, Searchable = true)]
    public string? Code { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200, Searchable = true)]
    public string? Email { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? Phone { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Address { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? City { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? Country { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? TaxNumber { get; private set; }

    public CustomerType CustomerType { get; private set; } = CustomerType.Company;
    public CustomerSegment Segment { get; private set; } = CustomerSegment.Standard;
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<ContactBase> Contacts { get; private set; } = [];
    public ICollection<OpportunityBase> Opportunities { get; private set; } = [];
    public ICollection<ActivityBase> Activities { get; private set; } = [];

    private CustomerBase() { }

    public static CustomerBase Create(
        string name, CustomerType type = CustomerType.Company,
        string? code = null, string? email = null, string? phone = null,
        string? address = null, string? city = null, string? country = null,
        string? taxNumber = null, CustomerSegment segment = CustomerSegment.Standard)
    {
        return new CustomerBase
        {
            Id = Guid.NewGuid(),
            Name = name, CustomerType = type, Code = code,
            Email = email, Phone = phone, Address = address,
            City = city, Country = country, TaxNumber = taxNumber,
            Segment = segment
        };
    }

    public void UpdateSegment(CustomerSegment segment) => Segment = segment;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
