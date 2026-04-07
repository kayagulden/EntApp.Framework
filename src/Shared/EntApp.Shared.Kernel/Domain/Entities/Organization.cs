using EntApp.Shared.Kernel.Domain.Ids;

namespace EntApp.Shared.Kernel.Domain.Entities;

/// <summary>
/// Organizasyon entity — hiyerarşik organizasyon yapısı.
/// Self-referencing tree (ParentId ile).
/// Shared Kernel referans verisi — tüm modüller tarafından kullanılır.
/// </summary>
public sealed class Organization : AuditableEntity<OrganizationId>
{
    /// <summary>Organizasyon adı.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Kısa kod (ör: "HQ", "TR-IST").</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Üst organizasyon ID (root için null).</summary>
    public OrganizationId? ParentId { get; private set; }

    /// <summary>Üst organizasyon navigasyonu.</summary>
    public Organization? Parent { get; private set; }

    /// <summary>Alt organizasyonlar.</summary>
    private readonly List<Organization> _children = [];
    public IReadOnlyCollection<Organization> Children => _children.AsReadOnly();

    /// <summary>Departmanlar.</summary>
    private readonly List<Department> _departments = [];
    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; private set; } = true;

    private Organization() { } // EF Core

    public static Organization Create(string name, string code, OrganizationId? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new Organization
        {
            Id = EntityId.New<OrganizationId>(),
            Name = name,
            Code = code.ToUpperInvariant(),
            ParentId = parentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Name = name;
        Code = code.ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
