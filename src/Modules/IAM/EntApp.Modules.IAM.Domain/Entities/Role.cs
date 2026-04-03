using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.IAM.Domain.Entities;

/// <summary>
/// IAM Role entity — permission koleksiyonu.
/// Kullanıcılara roller atanır, roller üzerinden yetkiler çözümlenir.
/// </summary>
public sealed class Role : AuditableEntity<Guid>
{
    /// <summary>Rol adı (ör: "Admin", "Manager").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Görüntüleme adı.</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>Açıklama.</summary>
    public string? Description { get; private set; }

    /// <summary>Sistem rolü mü? (silinemez, düzenlenemez)</summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>Rol-Permission ilişkisi.</summary>
    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    /// <summary>Kullanıcı-Rol ilişkisi.</summary>
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role() { } // EF Core

    public static Role Create(string name, string displayName, string? description = null, bool isSystemRole = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = description,
            IsSystemRole = isSystemRole,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if (_rolePermissions.Any(rp => rp.PermissionId == permission.Id)) return;

        _rolePermissions.Add(new RolePermission { RoleId = Id, PermissionId = permission.Id });
    }

    public void RemovePermission(Guid permissionId)
    {
        var rp = _rolePermissions.FirstOrDefault(x => x.PermissionId == permissionId);
        if (rp is not null) _rolePermissions.Remove(rp);
    }

    public void Update(string displayName, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>Many-to-many join entity: Role ↔ Permission.</summary>
public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
