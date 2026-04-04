using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// DI registration extension'ları.
/// </summary>
public static class DynamicCrudServiceCollectionExtensions
{
    /// <summary>
    /// DynamicCrud servislerini DI container'a kaydeder.
    /// Program.cs'de: builder.Services.AddDynamicCrud(assemblies);
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="assemblies">DynamicEntity attribute'ü taranacak assembly'ler.</param>
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

        // Scoped DbContext provider
        services.AddScoped<IDynamicDbContextProvider, DynamicDbContextProvider>();

        // Scoped CRUD service
        services.AddScoped<IDynamicCrudService, DynamicCrudService>();

        return services;
    }

    /// <summary>
    /// Modül DbContext'ini dynamic CRUD sistemi için kaydeder.
    /// Modül installer'da: services.AddDynamicDbContext&lt;IamDbContext&gt;(typeof(User), typeof(Role));
    /// </summary>
    /// <typeparam name="TDbContext">Modül DbContext tipi.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="entityTypes">Bu DbContext'e ait dynamic entity tipleri.</param>
    public static IServiceCollection AddDynamicDbContext<TDbContext>(
        this IServiceCollection services,
        params Type[] entityTypes)
        where TDbContext : DbContext
    {
        // Entity-DbContext eşleştirmesini bir initializer action olarak kaydet
        // Bu action scoped provider oluşturulduğunda çalışacak
        services.AddScoped<IDynamicDbContextProvider>(sp =>
        {
            var provider = new DynamicDbContextProvider(sp);
            foreach (var entityType in entityTypes)
            {
                provider.Register(entityType, typeof(TDbContext));
            }
            return provider;
        });

        return services;
    }
}
