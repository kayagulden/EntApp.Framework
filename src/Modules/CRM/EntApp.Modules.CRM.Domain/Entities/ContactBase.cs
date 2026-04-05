using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.CRM.Domain.Entities;

/// <summary>Müşteri ilgili kişisi.</summary>
[DynamicEntity("Contact", MenuGroup = "CRM")]
public sealed class ContactBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.Lookup, Required = true)]
    [DynamicLookup(typeof(CustomerBase), DisplayField = "Name")]
    public Guid CustomerId { get; private set; }

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 100, Searchable = true)]
    public string FirstName { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 100, Searchable = true)]
    public string LastName { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? Title { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200, Searchable = true)]
    public string? Email { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? Phone { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? Department { get; private set; }

    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsPrimary { get; private set; }

    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public CustomerBase Customer { get; private set; } = null!;

    private ContactBase() { }

    public static ContactBase Create(
        Guid customerId, string firstName, string lastName,
        string? title = null, string? email = null, string? phone = null,
        string? department = null, bool isPrimary = false)
    {
        return new ContactBase
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId, FirstName = firstName, LastName = lastName,
            Title = title, Email = email, Phone = phone,
            Department = department, IsPrimary = isPrimary
        };
    }
}
