using System.Collections.Concurrent;
using System.Reflection;
using EntApp.Shared.Infrastructure.DynamicCrud.Export;
using EntApp.Shared.Infrastructure.DynamicCrud.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// DI registration extension'ları.
/// </summary>
public static class DynamicCrudServiceCollectionExtensions
{
    /// <summary>
    /// Entity → DbContext eşleştirmesini tutan paylaşımlı (static) harita.
    /// Birden fazla AddDynamicDbContext çağrısı birbirini ezmez.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type> EntityDbContextMap = new();

    /// <summary>
    /// DynamicCrud servislerini DI container'a kaydeder.
    /// Program.cs'de: builder.Services.AddDynamicCrud(assemblies);
    /// </summary>
    public static IServiceCollection AddDynamicCrud(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // Singleton registry — startup'ta assembly taraması yapar
        services.AddSingleton<IDynamicEntityRegistry>(sp =>
        {
            var registry = new DynamicEntityRegistry(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DynamicEntityRegistry>>());
            registry.ScanAssemblies(assemblies);
            return registry;
        });

        // Singleton metadata service (cache'li)
        services.AddSingleton<IMetadataService>(sp =>
            new MetadataService(sp.GetRequiredService<IDynamicEntityRegistry>()));

        // Scoped DbContext provider — static map'ten okur
        services.AddScoped<IDynamicDbContextProvider>(sp =>
        {
            var provider = new DynamicDbContextProvider(sp);
            foreach (var (entityType, dbContextType) in EntityDbContextMap)
            {
                provider.Register(entityType, dbContextType);
            }
            return provider;
        });

        // Scoped CRUD service
        services.AddScoped<IDynamicCrudService, DynamicCrudService>();

        // Scoped Export/Import services
        services.AddScoped<IDynamicExportService, DynamicExportService>();
        services.AddScoped<ExportTemplateBuilder>();
        services.AddScoped<IDynamicImportService, DynamicImportService>();

        return services;
    }

    /// <summary>
    /// Modül DbContext'ini dynamic CRUD sistemi için kaydeder.
    /// Birden fazla kez çağrılabilir — kayıtlar birikir, birbirini ezmez.
    /// </summary>
    public static IServiceCollection AddDynamicDbContext<TDbContext>(
        this IServiceCollection services,
        params Type[] entityTypes)
        where TDbContext : DbContext
    {
        foreach (var entityType in entityTypes)
        {
            EntityDbContextMap[entityType] = typeof(TDbContext);
        }

        return services;
    }
}
