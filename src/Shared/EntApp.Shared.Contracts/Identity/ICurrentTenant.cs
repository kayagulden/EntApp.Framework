namespace EntApp.Shared.Contracts.Identity;

/// <summary>
/// Mevcut tenant bilgisine erişim kontratı.
/// TenantResolutionMiddleware tarafından çözümlenir.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>Aktif tenant kimliği.</summary>
    Guid TenantId { get; }

    /// <summary>Aktif tenant adı.</summary>
    string TenantName { get; }

    /// <summary>Tenant bilgisi mevcut mu? (Host context'te false olabilir)</summary>
    bool IsAvailable { get; }
}
