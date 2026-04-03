using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.IAM.Domain.Entities;

/// <summary>
/// IAM Department entity — organizasyona bağlı departman.
/// </summary>
public sealed class Department : AuditableEntity<Guid>
{
    /// <summary>Departman adı.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Departman kodu.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Bağlı organizasyon.</summary>
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = null!;

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; private set; } = true;

    private Department() { } // EF Core

    public static Department Create(string name, string code, Guid organizationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code.ToUpperInvariant(),
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
