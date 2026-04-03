using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Audit.Domain.Entities;

/// <summary>Audit log düzeyi.</summary>
public enum AuditAction
{
    Create,
    Update,
    Delete,
    Login,
    Logout,
    AccessDenied,
    Export,
    Import,
    Custom
}

/// <summary>
/// Genel denetim kaydı — tüm kullanıcı/sistem işlemlerini loglar.
/// </summary>
public class AuditLog : BaseEntity<Guid>
{
    /// <summary>İşlemi yapan kullanıcı ID.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>İşlemi yapan kullanıcı adı.</summary>
    public string? UserName { get; private set; }

    /// <summary>İşlem türü.</summary>
    public AuditAction Action { get; private set; }

    /// <summary>Etkilenen entity tipi (örn: "User", "Role").</summary>
    public string? EntityType { get; private set; }

    /// <summary>Etkilenen entity ID.</summary>
    public string? EntityId { get; private set; }

    /// <summary>Eski değerler (JSON).</summary>
    public string? OldValues { get; private set; }

    /// <summary>Yeni değerler (JSON).</summary>
    public string? NewValues { get; private set; }

    /// <summary>Değişen property listesi (JSON array).</summary>
    public string? AffectedColumns { get; private set; }

    /// <summary>IP adresi.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>User-Agent.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>İşlem zamanı (UTC).</summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>Ek açıklama.</summary>
    public string? Description { get; private set; }

    /// <summary>Tenant ID (multi-tenant).</summary>
    public Guid? TenantId { get; private set; }

    private AuditLog() { }

    public static AuditLog CreateEntityAudit(
        Guid? userId, string? userName,
        AuditAction action, string entityType, string entityId,
        string? oldValues, string? newValues, string? affectedColumns,
        string? ipAddress, Guid? tenantId)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            AffectedColumns = affectedColumns,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    public static AuditLog CreateActivityLog(
        Guid? userId, string? userName,
        AuditAction action, string description,
        string? ipAddress, string? userAgent, Guid? tenantId)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Action = action,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
            TenantId = tenantId
        };
    }
}
