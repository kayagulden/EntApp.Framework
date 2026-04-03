using System.Text.Json;
using EntApp.Modules.Audit.Domain.Entities;
using EntApp.Modules.Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EntApp.Modules.Audit.Infrastructure.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor — entity değişikliklerini otomatik olarak AuditLog tablosuna yazar.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly AuditDbContext _auditDb;

    public AuditSaveChangesInterceptor(AuditDbContext auditDb)
    {
        _auditDb = auditDb;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null || eventData.Context is AuditDbContext)
        {
            // Audit context'in kendisini loglamayı atla (infinite loop)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var entries = eventData.Context.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = CreateAuditEntry(entry);
            if (auditLog is not null)
            {
                _auditDb.AuditLogs.Add(auditLog);
            }
        }

        if (_auditDb.ChangeTracker.HasChanges())
        {
            await _auditDb.SaveChangesAsync(cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static AuditLog? CreateAuditEntry(EntityEntry entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = GetPrimaryKeyValue(entry);

        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => (AuditAction?)null
        };

        if (action is null) return null;

        string? oldValues = null;
        string? newValues = null;
        string? affectedColumns = null;

        var options = new JsonSerializerOptions { WriteIndented = false };

        switch (entry.State)
        {
            case EntityState.Added:
                newValues = JsonSerializer.Serialize(
                    entry.Properties
                        .Where(p => p.CurrentValue is not null)
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
                    options);
                break;

            case EntityState.Deleted:
                oldValues = JsonSerializer.Serialize(
                    entry.Properties
                        .Where(p => p.OriginalValue is not null)
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue),
                    options);
                break;

            case EntityState.Modified:
                var modifiedProps = entry.Properties
                    .Where(p => p.IsModified)
                    .ToList();

                oldValues = JsonSerializer.Serialize(
                    modifiedProps.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue),
                    options);
                newValues = JsonSerializer.Serialize(
                    modifiedProps.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
                    options);
                affectedColumns = JsonSerializer.Serialize(
                    modifiedProps.Select(p => p.Metadata.Name).ToList(),
                    options);
                break;
        }

        return AuditLog.CreateEntityAudit(
            userId: null, // HttpContext'ten alınacak — sonraki iterasyonda
            userName: null,
            action: action.Value,
            entityType: entityType,
            entityId: entityId ?? "unknown",
            oldValues: oldValues,
            newValues: newValues,
            affectedColumns: affectedColumns,
            ipAddress: null,
            tenantId: null);
    }

    private static string? GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProps = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .ToList();

        if (keyProps.Count == 1)
            return keyProps[0].CurrentValue?.ToString();

        return keyProps.Count > 0
            ? string.Join(",", keyProps.Select(p => p.CurrentValue?.ToString()))
            : null;
    }
}
