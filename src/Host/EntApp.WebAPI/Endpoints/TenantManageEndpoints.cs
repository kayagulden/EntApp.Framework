using EntApp.Shared.Contracts.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EntApp.WebAPI.Endpoints;

/// <summary>
/// Tenant-scoped admin endpoints — tenant admin'in kendi tenant'ını yönetmesi için.
/// Tüm endpoint'ler ICurrentTenant üzerinden otomatik filtrelenir.
/// </summary>
public static class TenantManageEndpoints
{
    public static void MapTenantManageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/manage")
            .WithTags("Tenant Management")
            .RequireAuthorization();

        // ── Tenant Info ──────────────────────────────────────
        group.MapGet("/info", async (
            ICurrentTenant currentTenant,
            EntApp.Modules.MultiTenancy.Infrastructure.Persistence.TenantDbContext db) =>
        {
            if (!currentTenant.IsAvailable)
                return Results.BadRequest(new { error = "Tenant context bulunamadı." });

            var tenantId = currentTenant.TenantId;
            var tenant = await db.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant is null)
                return Results.NotFound();

            return Results.Ok(new
            {
                tenant.Id,
                tenant.Name,
                Identifier = tenant.Identifier,
                Status = tenant.Status.ToString(),
                Plan = tenant.Plan,
            });
        })
        .WithName("GetTenantInfo")
        .WithDescription("Mevcut tenant bilgisini döndürür");

        // ── Tenant Settings ──────────────────────────────────
        group.MapGet("/settings", async (
            ICurrentTenant currentTenant,
            EntApp.Modules.Configuration.Infrastructure.Persistence.ConfigDbContext db) =>
        {
            Guid? tenantId = currentTenant.IsAvailable ? currentTenant.TenantId : null;

            var settings = await db.AppSettings
                .Where(s => s.TenantId == tenantId || s.TenantId == null)
                .OrderByDescending(s => s.TenantId) // tenant > global
                .ToListAsync();

            // Tenant override > Global fallback
            var merged = settings
                .GroupBy(s => s.Key)
                .Select(g => g.First())
                .Select(s => new
                {
                    s.Key,
                    s.Value,
                    s.Description,
                    IsGlobal = s.TenantId == null
                })
                .OrderBy(s => s.Key)
                .ToList();

            return Results.Ok(merged);
        })
        .WithName("GetTenantSettings")
        .WithDescription("Tenant bazlı konfigürasyon ayarlarını döndürür (tenant > global fallback)");

        // ── Tenant Feature Flags ─────────────────────────────
        group.MapGet("/feature-flags", async (
            ICurrentTenant currentTenant,
            EntApp.Modules.Configuration.Infrastructure.Persistence.ConfigDbContext db) =>
        {
            Guid? tenantId = currentTenant.IsAvailable ? currentTenant.TenantId : null;

            var flags = await db.FeatureFlags
                .Where(f => f.TenantId == tenantId || f.TenantId == null)
                .ToListAsync();

            var merged = flags
                .GroupBy(f => f.Name)
                .Select(g => g.OrderByDescending(f => f.TenantId).First())
                .Select(f => new
                {
                    f.Name,
                    f.DisplayName,
                    f.IsEnabled,
                    f.Description,
                    IsGlobal = f.TenantId == null
                })
                .OrderBy(f => f.Name)
                .ToList();

            return Results.Ok(merged);
        })
        .WithName("GetTenantFeatureFlags")
        .WithDescription("Tenant bazlı feature flag'leri döndürür");

        // ── Tenant UI Configs ────────────────────────────────
        group.MapGet("/ui-configs", async (
            ICurrentTenant currentTenant,
            EntApp.Modules.Configuration.Infrastructure.Persistence.ConfigDbContext db) =>
        {
            Guid? tenantId = currentTenant.IsAvailable ? currentTenant.TenantId : null;

            var configs = await db.DynamicUIConfigs
                .Where(c => c.TenantId == tenantId || c.TenantId == null)
                .Select(c => new
                {
                    c.Id,
                    c.EntityName,
                    c.TenantId,
                    c.ConfigJson,
                    IsGlobal = c.TenantId == null,
                    c.CreatedAt
                })
                .OrderBy(c => c.EntityName)
                .ToListAsync();

            return Results.Ok(configs);
        })
        .WithName("GetTenantUIConfigs")
        .WithDescription("Tenant bazlı UI konfigürasyonlarını döndürür");

        // ── Tenant Audit Logs ────────────────────────────────
        group.MapGet("/audit-logs", async (
            ICurrentTenant currentTenant,
            EntApp.Modules.Audit.Infrastructure.Persistence.AuditDbContext db,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? action = null,
            [FromQuery] string? entityName = null) =>
        {
            var tenantId = currentTenant.IsAvailable ? currentTenant.TenantId : (Guid?)null;

            var query = db.AuditLogs.AsQueryable();

            if (tenantId != null)
                query = query.Where(l => l.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(action)
                && Enum.TryParse<EntApp.Modules.Audit.Domain.Entities.AuditAction>(action, true, out var parsedAction))
                query = query.Where(l => l.Action == parsedAction);

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(l => l.EntityType == entityName);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.EntityType,
                    l.EntityId,
                    l.UserId,
                    l.UserName,
                    l.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new
            {
                items,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        })
        .WithName("GetTenantAuditLogs")
        .WithDescription("Tenant bazlı audit loglarını döndürür");
    }
}
