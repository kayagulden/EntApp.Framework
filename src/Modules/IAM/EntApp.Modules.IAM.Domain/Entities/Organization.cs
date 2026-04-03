using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.IAM.Domain.Entities;

/// <summary>
/// IAM Organization entity — hiyerarşik organizasyon yapısı.
/// Self-referencing tree (parentId ile).
/// </summary>
public sealed class Organization : AuditableEntity<Guid>
{
    /// <summary>Organizasyon adı.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Kısa kod (ör: "HQ", "TR-IST").</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Üst organizasyon ID (root için null).</summary>
    public Guid? ParentId { get; private set; }

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

    public static Organization Create(string name, string code, Guid? parentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new Organization
        {
            Id = Guid.NewGuid(),
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
}
