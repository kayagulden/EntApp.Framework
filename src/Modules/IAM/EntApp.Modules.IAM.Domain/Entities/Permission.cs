using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.IAM.Domain.Entities;

/// <summary>
/// IAM Permission entity — en granüler yetki birimi.
/// Örn: "users.create", "users.read", "orders.delete"
/// </summary>
public sealed class Permission : BaseEntity<Guid>
{
    /// <summary>İzin sistematik adı (ör: "users.create").</summary>
    public string SystemName { get; private set; } = null!;

    /// <summary>Görüntüleme adı (ör: "Kullanıcı Oluşturma").</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>Modül adı (ör: "IAM", "CMS").</summary>
    public string Module { get; private set; } = null!;

    /// <summary>Açıklama.</summary>
    public string? Description { get; private set; }

    private Permission() { } // EF Core

    public static Permission Create(string systemName, string displayName, string module, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        return new Permission
        {
            Id = Guid.NewGuid(),
            SystemName = systemName.ToLowerInvariant(),
            DisplayName = displayName,
            Module = module,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
