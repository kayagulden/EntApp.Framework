using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Veritabanı kaydı — ConfigurationItemBase'den türer.
/// Veritabanı motoruna özgü alanlar burada tanımlanır.
/// TPT: configuration_items (base) + databases (derived) tabloları.
/// Sunucu ilişkisi CIRelationship (HostedOn) ile yönetilir.
/// </summary>
[DynamicEntity("Database", MenuGroup = "CMDB")]
public sealed class DatabaseCI : ConfigurationItemBase
{
    public DatabaseEngine DatabaseEngine { get; private set; } = DatabaseEngine.PostgreSQL;

    [DynamicField(FieldType = FieldType.String, MaxLength = 50)]
    public string? Version { get; private set; }

    public int? Port { get; private set; }

    public decimal? SizeGB { get; private set; }

    /// <summary>Maskelenmiş veya vault referansı.</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 500)]
    public string? ConnectionString { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? BackupSchedule { get; private set; }

    /// <summary>Veritabanı yöneticisi.</summary>
    public Guid? AdminUserId { get; private set; }

    private DatabaseCI() { }

    public static DatabaseCI Create(string name, string code, string? description = null,
        DatabaseEngine databaseEngine = DatabaseEngine.PostgreSQL,
        CICriticality criticality = CICriticality.Medium,
        Guid? ownerUserId = null, Guid? adminUserId = null,
        string? version = null, int? port = null, decimal? sizeGB = null,
        string? connectionString = null, string? backupSchedule = null)
    {
        return new DatabaseCI
        {
            Id = EntityId.New<ConfigurationItemId>(),
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            DatabaseEngine = databaseEngine,
            Criticality = criticality,
            OwnerUserId = ownerUserId,
            AdminUserId = adminUserId,
            Version = version,
            Port = port,
            SizeGB = sizeGB,
            ConnectionString = connectionString,
            BackupSchedule = backupSchedule
        };
    }

    public void Update(string? name = null, string? description = null,
        DatabaseEngine? databaseEngine = null, CIStatus? status = null,
        CICriticality? criticality = null,
        Guid? ownerUserId = null, Guid? adminUserId = null,
        string? version = null, int? port = null, decimal? sizeGB = null,
        string? connectionString = null, string? backupSchedule = null)
    {
        UpdateBase(name, description, status, criticality, ownerUserId);
        if (databaseEngine.HasValue) DatabaseEngine = databaseEngine.Value;
        if (adminUserId.HasValue) AdminUserId = adminUserId.Value;
        if (version is not null) Version = version;
        if (port.HasValue) Port = port.Value;
        if (sizeGB.HasValue) SizeGB = sizeGB.Value;
        if (connectionString is not null) ConnectionString = connectionString;
        if (backupSchedule is not null) BackupSchedule = backupSchedule;
    }
}
