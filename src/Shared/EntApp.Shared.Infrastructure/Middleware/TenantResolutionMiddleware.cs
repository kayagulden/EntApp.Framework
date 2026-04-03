using EntApp.Shared.Contracts.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Middleware;

/// <summary>
/// Tenant çözümleme middleware.
/// Tenant bilgisini şu sırayla çözümler:
///   1. X-Tenant-Id header
///   2. Subdomain (tenant.example.com)
///   3. JWT claim (tenant_id)
/// Çözümlenen tenant bilgisi HttpContext.Items'a yazılır.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    /// <summary>Tenant ID header adı.</summary>
    public const string TenantIdHeader = "X-Tenant-Id";

    /// <summary>Tenant ID claim tipi.</summary>
    public const string TenantIdClaimType = "tenant_id";

    /// <summary>HttpContext.Items key'i.</summary>
    public const string TenantContextKey = "CurrentTenant";

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tenantInfo = ResolveTenant(context);

        if (tenantInfo is not null)
        {
            context.Items[TenantContextKey] = tenantInfo;
            _logger.LogDebug("Tenant resolved: {TenantId}", tenantInfo.TenantId);
        }

        await _next(context);
    }

    private static TenantInfo? ResolveTenant(HttpContext context)
    {
        // 1. Header'dan
        if (context.Request.Headers.TryGetValue(TenantIdHeader, out var headerValue)
            && Guid.TryParse(headerValue, out var headerId))
        {
            return new TenantInfo(headerId, $"tenant-{headerId:N}");
        }

        // 2. Subdomain'den (tenant.example.com)
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3) // subdomain.domain.tld
        {
            var subdomain = parts[0];
            if (subdomain != "www" && subdomain != "api")
            {
                // Subdomain'den tenant slug çözümleme
                // Gerçek implementasyonda tenant store'dan lookup yapılır
                return null; // Placeholder — tenant store entegrasyonunda implement edilecek
            }
        }

        // 3. JWT claim'den
        var claimValue = context.User?.FindFirst(TenantIdClaimType)?.Value;
        if (claimValue is not null && Guid.TryParse(claimValue, out var claimId))
        {
            return new TenantInfo(claimId, $"tenant-{claimId:N}");
        }

        return null;
    }

    /// <summary>
    /// Middleware tarafından çözümlenen tenant bilgisi.
    /// ICurrentTenant implementasyonu bu bilgiyi HttpContext.Items'dan okur.
    /// </summary>
    internal sealed record TenantInfo(Guid TenantId, string TenantName);
}
