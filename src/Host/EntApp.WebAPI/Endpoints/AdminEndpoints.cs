using EntApp.Modules.Audit.Domain.Entities;
using EntApp.Modules.Audit.Infrastructure.Persistence;
using EntApp.Modules.AI.Infrastructure.Persistence;
using EntApp.Modules.Configuration.Domain.Entities;
using EntApp.Modules.Configuration.Infrastructure.Persistence;
using EntApp.Modules.MultiTenancy.Domain.Entities;
using EntApp.Modules.MultiTenancy.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.DynamicCrud;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EntApp.WebAPI.Endpoints;

/// <summary>
/// Admin Panel — framework yönetim API'leri.
/// Tenant, Feature Flag, Audit, System Health, AI Stats, Cache yönetimi.
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").WithTags("Admin");

        // ═══════════════════════════════════════════════════════
        //  TENANT YÖNETİMİ
        // ═══════════════════════════════════════════════════════
        var tenants = admin.MapGroup("/tenants").WithTags("Admin - Tenants");

        tenants.MapGet("/", async (TenantDbContext db, string? status, int page = 1, int pageSize = 20) =>
        {
            var query = db.Tenants.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TenantStatus>(status, out var s))
                query = query.Where(t => t.Status == s);

            var total = await query.CountAsync();
            var items = await query.OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => new { t.Id, t.Name, t.Identifier, t.DisplayName,
                    Status = t.Status.ToString(), t.Plan, t.Subdomain,
                    t.AdminEmail, t.ActivatedAt, SettingCount = t.Settings.Count })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("AdminListTenants");

        tenants.MapGet("/{id:guid}", async (Guid id, TenantDbContext db) =>
        {
            var t = await db.Tenants.Include(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == id);
            return t is null ? Results.NotFound() : Results.Ok(t);
        }).WithName("AdminGetTenant");

        tenants.MapPost("/", async (CreateTenantRequest req, TenantDbContext db) =>
        {
            var tenant = Tenant.Create(req.Name, req.Identifier, req.AdminEmail,
                req.DisplayName, req.Description, req.Plan ?? "Free");
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/tenants/{tenant.Id}",
                new { tenant.Id, tenant.Identifier });
        }).WithName("AdminCreateTenant");

        tenants.MapPut("/{id:guid}", async (Guid id, UpdateTenantRequest req, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);
            if (tenant is null) return Results.NotFound();
            tenant.UpdateInfo(req.DisplayName, req.Description, req.LogoUrl);
            if (!string.IsNullOrEmpty(req.Subdomain)) tenant.SetSubdomain(req.Subdomain);
            if (!string.IsNullOrEmpty(req.Plan)) tenant.ChangePlan(req.Plan);
            await db.SaveChangesAsync();
            return Results.Ok(new { tenant.Id, Status = tenant.Status.ToString() });
        }).WithName("AdminUpdateTenant");

        tenants.MapPost("/{id:guid}/activate", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);
            if (tenant is null) return Results.NotFound();
            tenant.Activate();
            await db.SaveChangesAsync();
            return Results.Ok(new { tenant.Id, Status = tenant.Status.ToString() });
        }).WithName("AdminActivateTenant");

        tenants.MapPost("/{id:guid}/suspend", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);
            if (tenant is null) return Results.NotFound();
            tenant.Suspend();
            await db.SaveChangesAsync();
            return Results.Ok(new { tenant.Id, Status = tenant.Status.ToString() });
        }).WithName("AdminSuspendTenant");

        tenants.MapPost("/{id:guid}/deactivate", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FindAsync(id);
            if (tenant is null) return Results.NotFound();
            tenant.Deactivate();
            await db.SaveChangesAsync();
            return Results.Ok(new { tenant.Id, Status = tenant.Status.ToString() });
        }).WithName("AdminDeactivateTenant");

        tenants.MapGet("/{id:guid}/settings", async (Guid id, TenantDbContext db) =>
        {
            var settings = await db.Settings.Where(s => s.TenantId == id)
                .Select(s => new { s.Id, s.Key, s.Value })
                .ToListAsync();
            return Results.Ok(settings);
        }).WithName("AdminGetTenantSettings");

        tenants.MapPost("/{id:guid}/settings", async (Guid id, TenantSettingRequest req, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.Include(t => t.Settings)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null) return Results.NotFound();
            tenant.AddSetting(req.Key, req.Value);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Ayar kaydedildi." });
        }).WithName("AdminSetTenantSetting");

        // ═══════════════════════════════════════════════════════
        //  FEATURE FLAGS
        // ═══════════════════════════════════════════════════════
        var flags = admin.MapGroup("/feature-flags").WithTags("Admin - Feature Flags");

        flags.MapGet("/", async (ConfigDbContext db, Guid? tenantId) =>
        {
            var query = db.FeatureFlags.AsQueryable();
            if (tenantId.HasValue) query = query.Where(f => f.TenantId == tenantId.Value);

            var items = await query.OrderBy(f => f.Name)
                .Select(f => new { f.Id, f.Name, f.DisplayName, f.Description,
                    f.IsEnabled, f.TenantId, f.EnabledFrom, f.EnabledUntil,
                    EffectivelyEnabled = f.IsEnabled
                        && (!f.EnabledFrom.HasValue || DateTime.UtcNow >= f.EnabledFrom.Value)
                        && (!f.EnabledUntil.HasValue || DateTime.UtcNow <= f.EnabledUntil.Value) })
                .ToListAsync();
            return Results.Ok(items);
        }).WithName("AdminListFeatureFlags");

        flags.MapPost("/", async (CreateFeatureFlagRequest req, ConfigDbContext db) =>
        {
            var flag = FeatureFlag.Create(req.Name, req.DisplayName,
                req.Description, req.IsEnabled, req.TenantId);
            if (req.EnabledFrom.HasValue || req.EnabledUntil.HasValue)
                flag.SetSchedule(req.EnabledFrom, req.EnabledUntil);
            if (!string.IsNullOrEmpty(req.AllowedRoles))
                flag.SetAllowedRoles(req.AllowedRoles);
            db.FeatureFlags.Add(flag);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/feature-flags/{flag.Id}",
                new { flag.Id, flag.Name });
        }).WithName("AdminCreateFeatureFlag");

        flags.MapPost("/{id:guid}/toggle", async (Guid id, ConfigDbContext db) =>
        {
            var flag = await db.FeatureFlags.FindAsync(id);
            if (flag is null) return Results.NotFound();
            flag.Toggle();
            await db.SaveChangesAsync();
            return Results.Ok(new { flag.Id, flag.Name, flag.IsEnabled });
        }).WithName("AdminToggleFeatureFlag");

        flags.MapPut("/{id:guid}/schedule", async (Guid id, ScheduleFlagRequest req, ConfigDbContext db) =>
        {
            var flag = await db.FeatureFlags.FindAsync(id);
            if (flag is null) return Results.NotFound();
            flag.SetSchedule(req.EnabledFrom, req.EnabledUntil);
            await db.SaveChangesAsync();
            return Results.Ok(new { flag.Id, flag.EnabledFrom, flag.EnabledUntil });
        }).WithName("AdminScheduleFeatureFlag");

        flags.MapDelete("/{id:guid}", async (Guid id, ConfigDbContext db) =>
        {
            var flag = await db.FeatureFlags.FindAsync(id);
            if (flag is null) return Results.NotFound();
            db.FeatureFlags.Remove(flag);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"'{flag.Name}' silindi." });
        }).WithName("AdminDeleteFeatureFlag");

        // ═══════════════════════════════════════════════════════
        //  AUDIT VIEWER
        // ═══════════════════════════════════════════════════════
        var audit = admin.MapGroup("/audit-logs").WithTags("Admin - Audit");

        audit.MapGet("/", async (AuditDbContext db, Guid? userId, string? entityType,
            string? action, DateTime? from, DateTime? to,
            int page = 1, int pageSize = 30) =>
        {
            var query = db.AuditLogs.AsQueryable();
            if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
            if (!string.IsNullOrEmpty(entityType)) query = query.Where(a => a.EntityType == entityType);
            if (!string.IsNullOrEmpty(action) && Enum.TryParse<AuditAction>(action, out var act))
                query = query.Where(a => a.Action == act);
            if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
            if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new { a.Id, a.UserId, a.UserName,
                    Action = a.Action.ToString(), a.EntityType, a.EntityId,
                    a.Description, a.IpAddress, a.Timestamp, a.TenantId })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("AdminListAuditLogs");

        audit.MapGet("/{id:guid}", async (Guid id, AuditDbContext db) =>
        {
            var log = await db.AuditLogs.FindAsync(id);
            return log is null ? Results.NotFound() : Results.Ok(log);
        }).WithName("AdminGetAuditLog");

        audit.MapGet("/stats", async (AuditDbContext db, int days = 7) =>
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var stats = new
            {
                TotalLogs = await db.AuditLogs.CountAsync(a => a.Timestamp >= since),
                ByAction = await db.AuditLogs.Where(a => a.Timestamp >= since)
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key.ToString(), Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToListAsync(),
                ByEntity = await db.AuditLogs.Where(a => a.Timestamp >= since && a.EntityType != null)
                    .GroupBy(a => a.EntityType!)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(10).ToListAsync(),
                TopUsers = await db.AuditLogs.Where(a => a.Timestamp >= since && a.UserName != null)
                    .GroupBy(a => a.UserName!)
                    .Select(g => new { UserName = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(10).ToListAsync(),
                RecentLogins = await db.LoginRecords.Where(l => l.Timestamp >= since)
                    .GroupBy(l => l.Result)
                    .Select(g => new { Result = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync()
            };
            return Results.Ok(stats);
        }).WithName("AdminAuditStats").WithSummary("Audit istatistikleri");

        // ═══════════════════════════════════════════════════════
        //  SYSTEM HEALTH
        // ═══════════════════════════════════════════════════════
        var system = admin.MapGroup("/system").WithTags("Admin - System");

        system.MapGet("/health", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();
            var result = new
            {
                Status = report.Status.ToString(),
                Duration = report.TotalDuration.TotalMilliseconds,
                Entries = report.Entries.Select(e => new
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                    Duration = e.Value.Duration.TotalMilliseconds,
                    Description = e.Value.Description,
                    Error = e.Value.Exception?.Message,
                    Tags = e.Value.Tags
                })
            };
            return Results.Ok(result);
        }).WithName("AdminSystemHealth").WithSummary("Detaylı health check");

        system.MapGet("/modules", () =>
        {
            var modules = new[]
            {
                new { Name = "IAM", Schema = "iam", Status = "Active" },
                new { Name = "Configuration", Schema = "config", Status = "Active" },
                new { Name = "MultiTenancy", Schema = "tenant", Status = "Active" },
                new { Name = "Audit", Schema = "audit", Status = "Active" },
                new { Name = "Notification", Schema = "notify", Status = "Active" },
                new { Name = "FileManagement", Schema = "files", Status = "Active" },
                new { Name = "Localization", Schema = "i18n", Status = "Active" },
                new { Name = "Workflow", Schema = "wf", Status = "Active" },
                new { Name = "AI", Schema = "ai", Status = "Active" },
                new { Name = "CRM", Schema = "crm", Status = "Active" },
                new { Name = "HR", Schema = "hr", Status = "Active" },
                new { Name = "Finance", Schema = "fin", Status = "Active" },
                new { Name = "Inventory", Schema = "inv", Status = "Active" },
                new { Name = "Sales", Schema = "sales", Status = "Active" },
                new { Name = "Procurement", Schema = "proc", Status = "Active" },
                new { Name = "TaskManagement", Schema = "pm", Status = "Active" }
            };
            return Results.Ok(new { totalModules = modules.Length, modules });
        }).WithName("AdminListModules").WithSummary("Yüklü modül listesi");

        system.MapGet("/info", () =>
        {
            var info = new
            {
                Framework = "EntApp.Framework",
                Version = "1.0.0",
                Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
                WorkingSet = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024
            };
            return Results.Ok(info);
        }).WithName("AdminSystemInfo").WithSummary("Sistem bilgisi");

        // ═══════════════════════════════════════════════════════
        //  AI İSTATİSTİKLERİ
        // ═══════════════════════════════════════════════════════
        var aiStats = admin.MapGroup("/ai/stats").WithTags("Admin - AI Stats");

        aiStats.MapGet("/", async (AiDbContext db, int days = 30) =>
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var stats = new
            {
                TotalRequests = await db.AiUsageLogs.CountAsync(l => l.CreatedAt >= since),
                TotalTokens = await db.AiUsageLogs.Where(l => l.CreatedAt >= since)
                    .SumAsync(l => l.InputTokens + l.OutputTokens),
                TotalCost = await db.AiUsageLogs.Where(l => l.CreatedAt >= since)
                    .SumAsync(l => l.EstimatedCost),
                ByModel = await db.AiUsageLogs.Where(l => l.CreatedAt >= since)
                    .GroupBy(l => l.ModelName)
                    .Select(g => new {
                        Model = g.Key,
                        Requests = g.Count(),
                        InputTokens = g.Sum(l => l.InputTokens),
                        OutputTokens = g.Sum(l => l.OutputTokens),
                        Cost = g.Sum(l => l.EstimatedCost)
                    }).OrderByDescending(x => x.Requests).ToListAsync(),
                ByDay = await db.AiUsageLogs.Where(l => l.CreatedAt >= since)
                    .GroupBy(l => l.CreatedAt.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Requests = g.Count(),
                        Tokens = g.Sum(l => l.InputTokens + l.OutputTokens),
                        Cost = g.Sum(l => l.EstimatedCost)
                    }).OrderBy(x => x.Date).ToListAsync(),
                AvgLatency = await db.AiUsageLogs.Where(l => l.CreatedAt >= since)
                    .AverageAsync(l => (double?)l.DurationMs) ?? 0
            };
            return Results.Ok(stats);
        }).WithName("AdminAiStats").WithSummary("AI kullanım istatistikleri");

        aiStats.MapGet("/models", async (AiDbContext db) =>
        {
            var models = await db.AiModels.OrderBy(m => m.Provider).ThenBy(m => m.ModelName)
                .Select(m => new { m.Id, m.Provider, m.ModelName, m.DisplayName,
                    m.IsActive, m.MaxTokens, m.IsDefault, ModelType = m.ModelType.ToString() })
                .ToListAsync();
            return Results.Ok(models);
        }).WithName("AdminListAiModels");

        aiStats.MapGet("/prompts", async (AiDbContext db) =>
        {
            var prompts = await db.PromptTemplates.OrderBy(p => p.Key)
                .Select(p => new { p.Id, p.Key, p.Title, p.Category, p.Version,
                    p.IsActive, ContentLength = p.TemplateContent.Length,
                    p.CreatedAt, p.UpdatedAt })
                .ToListAsync();
            return Results.Ok(prompts);
        }).WithName("AdminListPrompts");

        // ═══════════════════════════════════════════════════════
        //  DYNAMIC UI CONFIG
        // ═══════════════════════════════════════════════════════
        var uiConfigs = admin.MapGroup("/ui-configs").WithTags("Admin - UI Config");

        uiConfigs.MapGet("/", async (ConfigDbContext db) =>
        {
            var items = await db.DynamicUIConfigs.AsNoTracking()
                .OrderBy(c => c.EntityName)
                .Select(c => new { c.Id, c.EntityName, c.TenantId, c.ConfigJson, c.CreatedAt, c.UpdatedAt })
                .ToListAsync();
            return Results.Ok(items);
        }).WithName("AdminListUIConfigs").WithSummary("Tüm UI override konfigürasyonları");

        uiConfigs.MapGet("/{entityName}", async (string entityName, ConfigDbContext db, Guid? tenantId = null) =>
        {
            var config = await db.DynamicUIConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.EntityName == entityName && c.TenantId == tenantId);
            return config is null
                ? Results.NotFound(new { error = $"'{entityName}' için UI config bulunamadı." })
                : Results.Ok(new { config.Id, config.EntityName, config.TenantId, config.ConfigJson, config.CreatedAt, config.UpdatedAt });
        }).WithName("AdminGetUIConfig");

        uiConfigs.MapPut("/{entityName}", async (
            string entityName,
            UpsertUIConfigRequest req,
            ConfigDbContext db,
            IDynamicUIConfigProvider configProvider) =>
        {
            var existing = await db.DynamicUIConfigs
                .FirstOrDefaultAsync(c => c.EntityName == entityName && c.TenantId == req.TenantId);

            if (existing is not null)
            {
                existing.UpdateConfig(req.ConfigJson);
            }
            else
            {
                existing = DynamicUIConfig.Create(entityName, req.ConfigJson, req.TenantId);
                db.DynamicUIConfigs.Add(existing);
            }

            await db.SaveChangesAsync();
            configProvider.InvalidateCache(entityName, req.TenantId);
            return Results.Ok(new { existing.Id, existing.EntityName, message = "UI config kaydedildi." });
        }).WithName("AdminUpsertUIConfig").WithSummary("Entity UI override konfigürasyonunu kaydet/güncelle");

        uiConfigs.MapDelete("/{entityName}", async (
            string entityName,
            ConfigDbContext db,
            IDynamicUIConfigProvider configProvider,
            Guid? tenantId = null) =>
        {
            var config = await db.DynamicUIConfigs
                .FirstOrDefaultAsync(c => c.EntityName == entityName && c.TenantId == tenantId);
            if (config is null)
                return Results.NotFound(new { error = $"'{entityName}' için UI config bulunamadı." });

            db.DynamicUIConfigs.Remove(config);
            await db.SaveChangesAsync();
            configProvider.InvalidateCache(entityName, tenantId);
            return Results.Ok(new { message = $"'{entityName}' UI config silindi — convention'a dönüldü." });
        }).WithName("AdminDeleteUIConfig").WithSummary("Entity UI override'ı sil (convention'a dön)");

        // ═══════════════════════════════════════════════════════
        //  CACHE YÖNETİMİ
        // ═══════════════════════════════════════════════════════
        var cache = admin.MapGroup("/cache").WithTags("Admin - Cache");

        cache.MapGet("/status", () =>
        {
            return Results.Ok(new
            {
                Provider = "InMemory (DistributedMemoryCache)",
                Note = "Redis geçişi sonrası detaylı istatistikler burada görünecek.",
                Status = "Active"
            });
        }).WithName("AdminCacheStatus");

        cache.MapPost("/clear/{key}", async (string key, IDistributedCache distributedCache) =>
        {
            await distributedCache.RemoveAsync(key);
            return Results.Ok(new { message = $"Cache key '{key}' temizlendi." });
        }).WithName("AdminClearCacheKey");

        return app;
    }
}

// ── Admin DTOs ─────────────────────────────────────────────
public sealed record CreateTenantRequest(string Name, string Identifier,
    string? AdminEmail = null, string? DisplayName = null,
    string? Description = null, string? Plan = null);

public sealed record UpdateTenantRequest(string? DisplayName = null,
    string? Description = null, string? LogoUrl = null,
    string? Subdomain = null, string? Plan = null);

public sealed record TenantSettingRequest(string Key, string Value);

public sealed record CreateFeatureFlagRequest(string Name, string DisplayName,
    string? Description = null, bool IsEnabled = false, Guid? TenantId = null,
    DateTime? EnabledFrom = null, DateTime? EnabledUntil = null,
    string? AllowedRoles = null);

public sealed record ScheduleFlagRequest(DateTime? EnabledFrom, DateTime? EnabledUntil);

public sealed record UpsertUIConfigRequest(string ConfigJson, Guid? TenantId = null);

