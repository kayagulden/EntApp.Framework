using EntApp.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EntApp.Shared.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges interceptor — Delete işlemlerini soft delete'e çevirir.
/// Entity'nin IsDeleted=true yapılır, fiziksel silme engellenir.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            ConvertDeleteToSoftDelete(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            ConvertDeleteToSoftDelete(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private static void ConvertDeleteToSoftDelete(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            // BaseEntity türevi mi kontrol et
            var entityType = entry.Entity.GetType();
            if (!IsBaseEntity(entityType))
            {
                continue;
            }

            // Fiziksel silme yerine soft delete
            entry.State = EntityState.Modified;
            entry.Property("IsDeleted").CurrentValue = true;
            entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
    }

    private static bool IsBaseEntity(Type type)
    {
        while (type is not null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BaseEntity<>))
            {
                return true;
            }

            type = type.BaseType!;
        }

        return false;
    }
}
