using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// Entity tipine göre doğru DbContext'i resolve eden provider.
/// Scoped lifetime ile kullanılır.
/// </summary>
public sealed class DynamicDbContextProvider : IDynamicDbContextProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _entityDbContextMap = new();

    public DynamicDbContextProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public DbContext GetDbContext(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);

        if (!_entityDbContextMap.TryGetValue(entityType, out var dbContextType))
        {
            throw new InvalidOperationException(
                $"No DbContext registered for entity type '{entityType.Name}'. " +
                $"Call services.AddDynamicDbContext<TDbContext>() in the module installer.");
        }

        return (DbContext)_serviceProvider.GetRequiredService(dbContextType);
    }

    public void Register(Type entityType, Type dbContextType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(dbContextType);

        _entityDbContextMap[entityType] = dbContextType;
    }

    public bool IsRegistered(Type entityType) => _entityDbContextMap.ContainsKey(entityType);
}
