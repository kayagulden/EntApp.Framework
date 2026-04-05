using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using EntApp.Shared.Infrastructure.DynamicCrud.Models;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// Reflection ile entity CLR tipinden metadata JSON schema üretir.
/// 3-tier fallback: DB override → Convention-based → Attribute.
/// Convention/Attribute tabanlı metadata cache'lenir (entity başına tek seferlik).
/// DB override'ları async çağrıda merge edilir.
/// </summary>
public sealed partial class MetadataService : IMetadataService
{
    private readonly IDynamicEntityRegistry _registry;
    private readonly IDynamicUIConfigProvider? _configProvider;
    private static readonly ConcurrentDictionary<string, EntityMetadataDto> _baseCache = new(StringComparer.OrdinalIgnoreCase);

    // BaseEntity/AuditableEntity'den gelen infrastructure property'leri — metadata'da gösterilmez
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(BaseEntity<Guid>.Id),
        nameof(BaseEntity<Guid>.CreatedAt),
        nameof(BaseEntity<Guid>.UpdatedAt),
        nameof(BaseEntity<Guid>.IsDeleted),
        nameof(BaseEntity<Guid>.RowVersion),
        nameof(AuditableEntity<Guid>.CreatedBy),
        nameof(AuditableEntity<Guid>.ModifiedBy)
    };

    public MetadataService(IDynamicEntityRegistry registry, IDynamicUIConfigProvider? configProvider = null)
    {
        _registry = registry;
        _configProvider = configProvider;
    }

    /// <summary>
    /// Sync metadata — sadece attribute/convention (geriye uyumluluk).
    /// </summary>
    public EntityMetadataDto? GetMetadata(string entityName)
    {
        return GetBaseMetadata(entityName);
    }

    /// <summary>
    /// Async metadata — DB override merge'li, 3-tier fallback.
    /// </summary>
    public async Task<EntityMetadataDto?> GetMetadataAsync(
        string entityName, Guid? tenantId = null, CancellationToken ct = default)
    {
        var baseMeta = GetBaseMetadata(entityName);
        if (baseMeta is null) return null;

        // DB override yoksa veya provider yoksa → base döndür
        if (_configProvider is null)
            return baseMeta;

        var overrideConfig = await _configProvider.GetOverrideAsync(entityName, tenantId, ct);
        if (overrideConfig is null)
            return baseMeta;

        return MergeOverride(baseMeta, overrideConfig);
    }

    public IReadOnlyList<MenuGroupDto> GetMenu()
    {
        var entities = _registry.GetAll()
            .Where(e => !e.IsDetail)
            .GroupBy(e => e.MenuGroup ?? "Genel")
            .OrderBy(g => g.Key)
            .Select(g => new MenuGroupDto
            {
                Name = g.Key,
                Items = g.OrderBy(e => e.Name)
                    .Select(e => new MenuItemDto
                    {
                        Entity = e.Name,
                        Title = HumanizePropertyName(e.Name)
                    })
                    .ToList()
            })
            .ToList();

        return entities;
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — Base Metadata (Attribute + Convention)
    // ═══════════════════════════════════════════════════════════

    private EntityMetadataDto? GetBaseMetadata(string entityName)
    {
        if (_baseCache.TryGetValue(entityName, out var cached))
            return cached;

        var info = _registry.GetByName(entityName);
        if (info is null) return null;

        var metadata = BuildMetadata(info);
        _baseCache.TryAdd(entityName, metadata);
        return metadata;
    }

    private EntityMetadataDto BuildMetadata(DynamicEntityInfo info)
    {
        var fields = BuildFieldMetadata(info.ClrType);
        var details = BuildDetailMetadata(info.ClrType);

        return new EntityMetadataDto
        {
            Entity = info.Name,
            Title = HumanizePropertyName(info.Name),
            MenuGroup = info.MenuGroup,
            IsDetail = info.IsDetail,
            Fields = fields,
            Details = details.Count > 0 ? details : null,
            Actions = new EntityActionsDto
            {
                Create = true,
                Edit = true,
                Delete = true,
                Export = true
            }
        };
    }

    private List<FieldMetadataDto> BuildFieldMetadata(Type type)
    {
        var fields = new List<FieldMetadataDto>();
        var order = 0;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !ExcludedProperties.Contains(p.Name));

        foreach (var prop in properties)
        {
            // Navigation property'leri atla (collection veya complex entity referansları)
            if (IsNavigationProperty(prop))
                continue;

            var fieldAttr = prop.GetCustomAttribute<DynamicFieldAttribute>();
            var lookupAttr = prop.GetCustomAttribute<DynamicLookupAttribute>();

            var fieldType = ResolveFieldType(prop, fieldAttr, lookupAttr);

            var field = new FieldMetadataDto
            {
                Name = ToCamelCase(prop.Name),
                Label = HumanizePropertyName(prop.Name),
                Type = fieldType,
                Required = fieldAttr?.Required ?? false,
                ReadOnly = fieldAttr?.ReadOnly ?? (fieldAttr?.Computed is not null),
                Searchable = fieldAttr?.Searchable ?? false,
                MaxLength = fieldAttr?.MaxLength ?? 0,
                MinLength = fieldAttr?.MinLength ?? 0,
                Min = fieldAttr is not null && fieldAttr.Min != double.MinValue ? fieldAttr.Min : null,
                Max = fieldAttr is not null && fieldAttr.Max != double.MaxValue ? fieldAttr.Max : null,
                DefaultValue = fieldAttr?.DefaultValue,
                Computed = fieldAttr?.Computed,
                ShowInList = true,
                Order = order++,
                Options = ResolveEnumOptions(prop),
                Lookup = lookupAttr is not null ? ResolveLookupInfo(lookupAttr) : null
            };

            fields.Add(field);
        }

        return fields;
    }

    private List<DetailMetadataDto> BuildDetailMetadata(Type type)
    {
        var details = new List<DetailMetadataDto>();

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<DynamicDetailAttribute>() is not null);

        foreach (var prop in properties)
        {
            var detailAttr = prop.GetCustomAttribute<DynamicDetailAttribute>()!;
            var detailFields = BuildFieldMetadata(detailAttr.DetailEntityType);

            details.Add(new DetailMetadataDto
            {
                Name = ToCamelCase(prop.Name),
                Label = HumanizePropertyName(prop.Name),
                Entity = _registry.GetByType(detailAttr.DetailEntityType)?.Name ?? detailAttr.DetailEntityType.Name,
                Fields = detailFields
            });
        }

        return details;
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — DB Override Merge
    // ═══════════════════════════════════════════════════════════

    private static EntityMetadataDto MergeOverride(EntityMetadataDto baseMeta, DynamicUIConfigOverrideDto overrideConfig)
    {
        // Entity-level override
        var title = overrideConfig.Title ?? baseMeta.Title;
        var icon = overrideConfig.Icon ?? baseMeta.Icon;

        // Actions override
        var actions = baseMeta.Actions;
        if (overrideConfig.Actions is not null)
        {
            actions = new EntityActionsDto
            {
                Create = overrideConfig.Actions.Create ?? actions.Create,
                Edit = overrideConfig.Actions.Edit ?? actions.Edit,
                Delete = overrideConfig.Actions.Delete ?? actions.Delete,
                Export = overrideConfig.Actions.Export ?? actions.Export
            };
        }

        // Field-level override
        var fields = baseMeta.Fields;
        if (overrideConfig.Fields is { Count: > 0 })
        {
            fields = baseMeta.Fields
                .Select(f =>
                {
                    if (!overrideConfig.Fields.TryGetValue(f.Name, out var fieldOverride))
                        return f;

                    return f with
                    {
                        Label = fieldOverride.Label ?? f.Label,
                        Order = fieldOverride.Order ?? f.Order,
                        Width = fieldOverride.Width ?? f.Width,
                        ShowInList = fieldOverride.ShowInList ?? f.ShowInList,
                        Hidden = fieldOverride.Hidden ?? f.Hidden,
                        Searchable = fieldOverride.Searchable ?? f.Searchable,
                        ReadOnly = fieldOverride.ReadOnly ?? f.ReadOnly,
                        Required = fieldOverride.Required ?? f.Required
                    };
                })
                .Where(f => !f.Hidden) // Hidden alanları metadata'dan çıkar
                .OrderBy(f => f.Order)
                .ToList();
        }

        return baseMeta with
        {
            Title = title,
            Icon = icon,
            Actions = actions,
            Fields = fields
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — Field Type Resolution
    // ═══════════════════════════════════════════════════════════

    private static string ResolveFieldType(PropertyInfo prop, DynamicFieldAttribute? fieldAttr, DynamicLookupAttribute? lookupAttr)
    {
        // Explicit lookup attribute → always lookup
        if (lookupAttr is not null)
            return "lookup";

        // Explicit field type from attribute
        if (fieldAttr?.FieldType is not null and not FieldType.Auto)
            return fieldAttr.FieldType.ToString().ToLowerInvariant();

        // Auto-detect from CLR type
        return InferFieldType(prop.PropertyType);
    }

    private static string InferFieldType(Type clrType)
    {
        // Nullable<T> → unwrap
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (underlyingType.IsEnum) return "enum";
        if (underlyingType == typeof(bool)) return "boolean";
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset)) return "datetime";
        if (underlyingType == typeof(DateOnly)) return "date";
        if (underlyingType == typeof(decimal)) return "decimal";
        if (underlyingType == typeof(double) || underlyingType == typeof(float)) return "decimal";
        if (underlyingType == typeof(int) || underlyingType == typeof(long) || underlyingType == typeof(short)) return "number";
        if (underlyingType == typeof(Guid)) return "string";

        return "string";
    }

    private static IReadOnlyList<string>? ResolveEnumOptions(PropertyInfo prop)
    {
        var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (!underlyingType.IsEnum) return null;

        return Enum.GetNames(underlyingType);
    }

    private LookupInfoDto ResolveLookupInfo(DynamicLookupAttribute attr)
    {
        var lookupEntityInfo = _registry.GetByType(attr.EntityType);
        var entityName = lookupEntityInfo?.Name ?? attr.EntityType.Name;

        return new LookupInfoDto
        {
            Entity = entityName,
            DisplayField = attr.DisplayField ?? "name",
            Endpoint = $"/api/v1/dynamic/{entityName}/lookup"
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIVATE — Helpers
    // ═══════════════════════════════════════════════════════════

    private static bool IsNavigationProperty(PropertyInfo prop)
    {
        var type = prop.PropertyType;

        // DynamicDetail attribute varsa navigation değil, detail olarak ayrı işlenir
        if (prop.GetCustomAttribute<DynamicDetailAttribute>() is not null)
            return true;

        // Collection (List<T>, ICollection<T>, IEnumerable<T> — string hariç)
        if (type != typeof(string) && type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IReadOnlyCollection<>) ||
                genericDef == typeof(IEnumerable<>))
            {
                return true;
            }
        }

        // Class-type navigation (entity reference, not Guid FK)
        // Guid, string, ValueObject'ler değil; başka entity referansları
        if (type.IsClass && type != typeof(string) && !type.IsPrimitive)
        {
            // Eğer DynamicLookup attrribute varsa bu FK guid'i, navigation değil
            if (prop.GetCustomAttribute<DynamicLookupAttribute>() is not null)
                return false;

            // BaseEntity veya subclass ise navigation property
            if (IsEntityType(type))
                return true;
        }

        return false;
    }

    private static bool IsEntityType(Type type)
    {
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(BaseEntity<>))
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// PascalCase property adını insan-okunabilir etikete çevirir.
    /// Örnek: "OrderNumber" → "Order Number", "CustomerId" → "Customer Id"
    /// </summary>
    private static string HumanizePropertyName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return PascalCaseSplitter().Replace(name, " $1").Trim();
    }

    [GeneratedRegex(@"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])")]
    private static partial Regex PascalCaseSplitter();
}
