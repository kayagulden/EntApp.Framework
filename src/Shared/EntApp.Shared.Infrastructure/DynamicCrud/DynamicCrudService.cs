using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// EF Core DbContext üzerinde reflection ile generic CRUD operasyonları gerçekleştirir.
/// Entity adına göre çalışır — compile-time generic constraint gerekmez.
/// </summary>
public sealed class DynamicCrudService : IDynamicCrudService
{
    private readonly IDynamicEntityRegistry _registry;
    private readonly IDynamicDbContextProvider _dbContextProvider;
    private readonly ILogger<DynamicCrudService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DynamicCrudService(
        IDynamicEntityRegistry registry,
        IDynamicDbContextProvider dbContextProvider,
        ILogger<DynamicCrudService> logger)
    {
        _registry = registry;
        _dbContextProvider = dbContextProvider;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  GET PAGED
    // ═══════════════════════════════════════════════════════════

    public async Task<PagedResult<JsonElement>> GetPagedAsync(
        string entityName, PagedRequest request, CancellationToken ct = default)
    {
        var (info, db, dbSet) = ResolveEntity(entityName);

        // AsNoTracking query via IQueryable
        var query = dbSet as IQueryable<object> ?? throw CreateSetError(entityName);

        // Search filter (searchable fields)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = ApplySearchFilter(query, info.ClrType, request.SearchTerm);
        }

        var totalCount = await CountAsync(query, ct);

        // Sorting
        query = ApplySorting(query, info.ClrType, request.SortBy, request.SortDescending);

        // Paging
        query = query.Skip(request.Skip).Take(request.Take);

        var items = await ToListAsync(query, ct);

        var jsonItems = items
            .Select(item => JsonSerializer.SerializeToElement(item, item.GetType(), JsonOptions))
            .ToList();

        _logger.LogDebug("[DynamicCrud] GetPaged {Entity}: {Count}/{Total}",
            entityName, jsonItems.Count, totalCount);

        return new PagedResult<JsonElement>
        {
            Items = jsonItems,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  GET BY ID
    // ═══════════════════════════════════════════════════════════

    public async Task<JsonElement?> GetByIdAsync(
        string entityName, Guid id, CancellationToken ct = default)
    {
        var (info, db, _) = ResolveEntity(entityName);

        var entity = await db.FindAsync(info.ClrType, [id], ct);
        if (entity is null) return null;

        // Soft delete kontrolü
        if (entity is BaseEntity<Guid> baseEntity && baseEntity.IsDeleted)
            return null;

        return JsonSerializer.SerializeToElement(entity, info.ClrType, JsonOptions);
    }

    // ═══════════════════════════════════════════════════════════
    //  CREATE
    // ═══════════════════════════════════════════════════════════

    public async Task<Guid> CreateAsync(
        string entityName, JsonElement body, CancellationToken ct = default)
    {
        var (info, db, _) = ResolveEntity(entityName);

        var entity = JsonSerializer.Deserialize(body.GetRawText(), info.ClrType, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize body for entity '{entityName}'.");

        // Id ata
        var idProp = info.ClrType.GetProperty("Id");
        if (idProp is not null)
        {
            var currentId = idProp.GetValue(entity);
            if (currentId is Guid guid && guid == Guid.Empty)
            {
                idProp.SetValue(entity, Guid.NewGuid());
            }
        }

        // CreatedAt set et
        SetPropertyIfExists(entity, "CreatedAt", DateTime.UtcNow);

        db.Add(entity);
        await db.SaveChangesAsync(ct);

        var newId = idProp?.GetValue(entity) as Guid? ?? Guid.Empty;
        _logger.LogInformation("[DynamicCrud] Created {Entity}: {Id}", entityName, newId);
        return newId;
    }

    // ═══════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════

    public async Task UpdateAsync(
        string entityName, Guid id, JsonElement body, CancellationToken ct = default)
    {
        var (info, db, _) = ResolveEntity(entityName);

        var existingEntity = await db.FindAsync(info.ClrType, [id], ct)
            ?? throw new KeyNotFoundException($"'{entityName}' with id '{id}' not found.");

        // body'deki property'leri mevcut entity'ye uygula
        ApplyJsonPatch(existingEntity, body, info.ClrType);

        // UpdatedAt set et
        SetPropertyIfExists(existingEntity, "UpdatedAt", DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("[DynamicCrud] Updated {Entity}: {Id}", entityName, id);
    }

    // ═══════════════════════════════════════════════════════════
    //  DELETE (Soft)
    // ═══════════════════════════════════════════════════════════

    public async Task DeleteAsync(
        string entityName, Guid id, CancellationToken ct = default)
    {
        var (info, db, _) = ResolveEntity(entityName);

        var entity = await db.FindAsync(info.ClrType, [id], ct)
            ?? throw new KeyNotFoundException($"'{entityName}' with id '{id}' not found.");

        // Soft delete
        SetPropertyIfExists(entity, "IsDeleted", true);
        SetPropertyIfExists(entity, "UpdatedAt", DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("[DynamicCrud] Deleted (soft) {Entity}: {Id}", entityName, id);
    }

    // ═══════════════════════════════════════════════════════════
    //  LOOKUP
    // ═══════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<LookupDto>> LookupAsync(
        string entityName, string? search = null, int take = 20, CancellationToken ct = default)
    {
        var (info, db, dbSet) = ResolveEntity(entityName);
        var query = dbSet as IQueryable<object> ?? throw CreateSetError(entityName);

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplySearchFilter(query, info.ClrType, search);
        }

        query = query.Take(Math.Clamp(take, 1, 100));

        var items = await ToListAsync(query, ct);

        // LookupDto'ya dönüştür
        var displayProp = FindDisplayProperty(info.ClrType);
        var idProp = info.ClrType.GetProperty("Id")!;

        return items.Select(item => new LookupDto
        {
            Id = (Guid)(idProp.GetValue(item) ?? Guid.Empty),
            Text = displayProp?.GetValue(item)?.ToString() ?? item.ToString() ?? "",
            IsActive = GetBoolProperty(item, "IsActive") ?? true
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — Entity Resolution
    // ═══════════════════════════════════════════════════════════

    private (DynamicEntityInfo info, DbContext db, object dbSet) ResolveEntity(string entityName)
    {
        var info = _registry.GetByName(entityName)
            ?? throw new KeyNotFoundException($"Dynamic entity '{entityName}' not found in registry.");

        var db = _dbContextProvider.GetDbContext(info.ClrType);

        // DbContext.Set<TEntity>() via reflection
        var setMethod = typeof(DbContext).GetMethods()
            .First(m => m.Name == "Set" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);

        var genericSetMethod = setMethod.MakeGenericMethod(info.ClrType);
        var dbSet = genericSetMethod.Invoke(db, null)
            ?? throw new InvalidOperationException($"DbContext.Set<{info.ClrType.Name}>() returned null.");

        return (info, db, dbSet);
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — Query Helpers
    // ═══════════════════════════════════════════════════════════

    private static IQueryable<object> ApplySearchFilter(IQueryable<object> query, Type entityType, string searchTerm)
    {
        var searchLower = searchTerm.ToLowerInvariant();

        // DynamicField(Searchable=true) olan veya string tipli property'lerde arama
        var searchableProps = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p =>
            {
                if (p.PropertyType != typeof(string)) return false;
                var fieldAttr = p.GetCustomAttribute<DynamicFieldAttribute>();
                return fieldAttr?.Searchable == true
                    || p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)
                    || p.Name.Equals("Code", StringComparison.OrdinalIgnoreCase)
                    || p.Name.Equals("Title", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (searchableProps.Count == 0) return query;

        // Build: x => x.Prop1.ToLower().Contains(search) || x.Prop2.ToLower().Contains(search) ...
        var param = Expression.Parameter(typeof(object), "x");
        var castExpr = Expression.Convert(param, entityType);

        Expression? combinedPredicate = null;

        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
        var searchConstant = Expression.Constant(searchLower);

        foreach (var prop in searchableProps)
        {
            var propAccess = Expression.Property(castExpr, prop);
            var nullCheck = Expression.NotEqual(propAccess, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(propAccess, toLowerMethod);
            var contains = Expression.Call(toLower, containsMethod, searchConstant);
            var withNullCheck = Expression.AndAlso(nullCheck, contains);

            combinedPredicate = combinedPredicate is null
                ? withNullCheck
                : Expression.OrElse(combinedPredicate, withNullCheck);
        }

        if (combinedPredicate is null) return query;

        var lambda = Expression.Lambda<Func<object, bool>>(combinedPredicate, param);
        return query.Where(lambda);
    }

    private static IQueryable<object> ApplySorting(IQueryable<object> query, Type entityType, string? sortBy, bool descending)
    {
        var propertyName = sortBy ?? "CreatedAt";
        var prop = entityType.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (prop is null)
        {
            // Fallback: Id'ye göre sırala
            prop = entityType.GetProperty("Id") ?? entityType.GetProperties().First();
        }

        var param = Expression.Parameter(typeof(object), "x");
        var castExpr = Expression.Convert(param, entityType);
        var propAccess = Expression.Property(castExpr, prop);
        var converted = Expression.Convert(propAccess, typeof(object));
        var keySelector = Expression.Lambda<Func<object, object>>(converted, param);

        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    private static void ApplyJsonPatch(object entity, JsonElement body, Type entityType)
    {
        foreach (var jsonProp in body.EnumerateObject())
        {
            // Id, CreatedAt, IsDeleted gibi alanlara dokunma
            if (jsonProp.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                jsonProp.Name.Equals("createdAt", StringComparison.OrdinalIgnoreCase) ||
                jsonProp.Name.Equals("isDeleted", StringComparison.OrdinalIgnoreCase) ||
                jsonProp.Name.Equals("rowVersion", StringComparison.OrdinalIgnoreCase) ||
                jsonProp.Name.Equals("createdBy", StringComparison.OrdinalIgnoreCase))
                continue;

            var prop = entityType.GetProperty(jsonProp.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is null || !prop.CanWrite) continue;

            try
            {
                var value = JsonSerializer.Deserialize(jsonProp.Value.GetRawText(), prop.PropertyType, JsonOptions);
                prop.SetValue(entity, value);
            }
            catch
            {
                // Dönüştürülemeyen alanları sessizce atla
            }
        }
    }

    private static void SetPropertyIfExists(object entity, string propertyName, object value)
    {
        var prop = entity.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        if (prop is not null && prop.CanWrite)
        {
            prop.SetValue(entity, value);
        }
    }

    private static PropertyInfo? FindDisplayProperty(Type entityType)
    {
        return entityType.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? entityType.GetProperty("Title", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? entityType.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?? entityType.GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }

    private static bool? GetBoolProperty(object entity, string propName)
    {
        var prop = entity.GetType().GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        return prop?.GetValue(entity) as bool?;
    }

    private static async Task<int> CountAsync(IQueryable<object> query, CancellationToken ct)
    {
        try
        {
            return await query.CountAsync(ct);
        }
        catch
        {
            // Fallback — in-memory count
            return query.Count();
        }
    }

    private static async Task<List<object>> ToListAsync(IQueryable<object> query, CancellationToken ct)
    {
        try
        {
            return await query.ToListAsync(ct);
        }
        catch
        {
            // Fallback — in-memory
            return query.ToList();
        }
    }

    private static InvalidOperationException CreateSetError(string entityName) =>
        new($"Could not obtain DbSet for entity '{entityName}'. Ensure the entity is registered in a DbContext.");
}
