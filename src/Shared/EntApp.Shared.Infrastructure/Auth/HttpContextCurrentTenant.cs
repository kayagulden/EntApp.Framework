using EntApp.Shared.Contracts.Identity;
using EntApp.Shared.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;

namespace EntApp.Shared.Infrastructure.Auth;

/// <summary>
/// HttpContext üzerinden TenantResolutionMiddleware tarafından çözümlenen
/// tenant bilgisini okur.
/// </summary>
public sealed class HttpContextCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private TenantResolutionMiddleware.TenantInfo? TenantInfo =>
        _httpContextAccessor.HttpContext?.Items[TenantResolutionMiddleware.TenantContextKey]
            as TenantResolutionMiddleware.TenantInfo;

    public Guid TenantId => TenantInfo?.TenantId ?? Guid.Empty;

    public string TenantName => TenantInfo?.TenantName ?? string.Empty;

    public bool IsAvailable => TenantInfo is not null;
}
