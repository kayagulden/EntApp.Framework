using System.Collections.Concurrent;
using System.Text.Json;
using EntApp.Modules.Configuration.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.DynamicCrud;
using EntApp.Shared.Infrastructure.DynamicCrud.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Configuration.Infrastructure.DynamicUI;

/// <summary>
/// DynamicUIConfigs tablosundan entity bazlı UI override konfigürasyonunu sağlar.
/// Tenant > Global fallback mantığı ile çalışır.
/// In-memory cache ile performans optimize edilir.
/// </summary>
public sealed class DynamicUIConfigProvider : IDynamicUIConfigProvider
{
    private readonly ConfigDbContext _db;
    private readonly ILogger<DynamicUIConfigProvider> _logger;
    private readonly ConcurrentDictionary<string, DynamicUIConfigOverrideDto?> _cache = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DynamicUIConfigProvider(ConfigDbContext db, ILogger<DynamicUIConfigProvider> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DynamicUIConfigOverrideDto?> GetOverrideAsync(
        string entityName, Guid? tenantId, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(entityName, tenantId);

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        // Tenant-specific override'ı ara, yoksa global'e düş
        var config = tenantId.HasValue
            ? await _db.DynamicUIConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.EntityName == entityName && c.TenantId == tenantId, ct)
              ?? await _db.DynamicUIConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.EntityName == entityName && c.TenantId == null, ct)
            : await _db.DynamicUIConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.EntityName == entityName && c.TenantId == null, ct);

        if (config is null)
        {
            _cache.TryAdd(cacheKey, null);
            return null;
        }

        try
        {
            var overrideDto = JsonSerializer.Deserialize<DynamicUIConfigOverrideDto>(config.ConfigJson, JsonOptions);
            _cache.TryAdd(cacheKey, overrideDto);
            return overrideDto;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "[DynamicUI] Failed to deserialize ConfigJson for entity '{EntityName}' (id: {Id})",
                entityName, config.Id);
            return null;
        }
    }

    public void InvalidateCache(string entityName, Guid? tenantId = null)
    {
        // Entity adı ile eşleşen tüm cache kayıtlarını temizle
        var keysToRemove = _cache.Keys
            .Where(k => k.StartsWith($"{entityName}:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug("[DynamicUI] Cache invalidated for entity '{EntityName}' ({Count} keys removed)",
            entityName, keysToRemove.Count);
    }

    private static string BuildCacheKey(string entityName, Guid? tenantId)
        => $"{entityName}:{tenantId?.ToString() ?? "global"}";
}
