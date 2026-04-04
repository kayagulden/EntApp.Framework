using System.Reflection;
using EntApp.Shared.Kernel.Domain.Attributes;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// Assembly taraması ile [DynamicEntity] attribute'ü olan entity'leri keşfeder ve kaydeder.
/// Singleton olarak yaşar.
/// </summary>
public sealed class DynamicEntityRegistry : IDynamicEntityRegistry
{
    private readonly Dictionary<string, DynamicEntityInfo> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, DynamicEntityInfo> _byType = new();
    private readonly ILogger<DynamicEntityRegistry> _logger;

    public DynamicEntityRegistry(ILogger<DynamicEntityRegistry> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<DynamicEntityInfo> GetAll() => _byName.Values.ToList();

    public DynamicEntityInfo? GetByName(string entityName)
    {
        _byName.TryGetValue(entityName, out var info);
        return info;
    }

    public DynamicEntityInfo? GetByType(Type entityType)
    {
        _byType.TryGetValue(entityType, out var info);
        return info;
    }

    public void ScanAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly);
        }

        _logger.LogInformation(
            "[DynamicCrud] Registry scan complete — {Count} dynamic entities registered: [{Entities}]",
            _byName.Count,
            string.Join(", ", _byName.Keys));
    }

    private void ScanAssembly(Assembly assembly)
    {
        var entityTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                     && t.GetCustomAttribute<DynamicEntityAttribute>() is not null);

        foreach (var type in entityTypes)
        {
            var attr = type.GetCustomAttribute<DynamicEntityAttribute>()!;

            if (_byName.ContainsKey(attr.Name))
            {
                _logger.LogWarning(
                    "[DynamicCrud] Duplicate dynamic entity name '{Name}' — type {Type} skipped (already registered as {ExistingType})",
                    attr.Name, type.FullName, _byName[attr.Name].ClrType.FullName);
                continue;
            }

            var info = new DynamicEntityInfo
            {
                Name = attr.Name,
                ClrType = type,
                MenuGroup = attr.MenuGroup,
                IsDetail = attr.IsDetail,
                Attribute = attr
            };

            _byName[attr.Name] = info;
            _byType[type] = info;

            _logger.LogDebug(
                "[DynamicCrud] Registered dynamic entity: {Name} → {Type} (group: {Group}, detail: {IsDetail})",
                attr.Name, type.FullName, attr.MenuGroup ?? "Genel", attr.IsDetail);
        }
    }
}
