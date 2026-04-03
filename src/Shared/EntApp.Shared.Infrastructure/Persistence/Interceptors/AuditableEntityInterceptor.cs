using EntApp.Shared.Contracts.Identity;
using EntApp.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EntApp.Shared.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges interceptor — AuditableEntity alanlarını otomatik set eder.
/// CreatedAt/CreatedBy (Add), UpdatedAt/ModifiedBy (Modified).
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser? _currentUser;

    public AuditableEntityInterceptor(ICurrentUser? currentUser = null)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            UpdateAuditFields(eventData.Context);
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
            UpdateAuditFields(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void UpdateAuditFields(DbContext context)
    {
        var now = DateTime.UtcNow;
        var userName = _currentUser?.IsAuthenticated == true ? _currentUser.UserName : "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // BaseEntity — CreatedAt, UpdatedAt
            if (entry.Entity.GetType().BaseType is { IsGenericType: true } baseType
                && baseType.GetGenericTypeDefinition() == typeof(BaseEntity<>))
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Property("CreatedAt").CurrentValue = now;
                        break;
                    case EntityState.Modified:
                        entry.Property("UpdatedAt").CurrentValue = now;
                        break;
                }
            }

            // AuditableEntity — CreatedBy, ModifiedBy
            if (entry.Entity.GetType().BaseType is { IsGenericType: true } auditBaseType
                && IsAuditableEntity(auditBaseType))
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Property("CreatedBy").CurrentValue = userName;
                        break;
                    case EntityState.Modified:
                        entry.Property("ModifiedBy").CurrentValue = userName;
                        break;
                }
            }
        }
    }

    private static bool IsAuditableEntity(Type type)
    {
        while (type is not null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AuditableEntity<>))
            {
                return true;
            }

            type = type.BaseType!;
        }

        return false;
    }
}
