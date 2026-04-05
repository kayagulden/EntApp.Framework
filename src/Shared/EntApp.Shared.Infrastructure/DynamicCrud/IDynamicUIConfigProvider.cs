using EntApp.Shared.Infrastructure.DynamicCrud.Models;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// DynamicUIConfigs tablosundan entity bazlı UI override konfigürasyonunu sağlar.
/// Tenant > Global fallback mantığı ile çalışır.
/// </summary>
public interface IDynamicUIConfigProvider
{
    /// <summary>
    /// Entity için UI override konfigürasyonunu döner (tenant > global fallback).
    /// Override yoksa null döner.
    /// </summary>
    Task<DynamicUIConfigOverrideDto?> GetOverrideAsync(string entityName, Guid? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Cache'lenmiş override'ı invalidate eder.
    /// Config güncellendiğinde çağrılır.
    /// </summary>
    void InvalidateCache(string entityName, Guid? tenantId = null);
}
