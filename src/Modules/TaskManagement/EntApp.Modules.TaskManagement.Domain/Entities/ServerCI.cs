using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Sunucu kaydı — ConfigurationItemBase'den türer.
/// Fiziksel/sanal/cloud sunuculara özgü alanlar burada tanımlanır.
/// TPT: configuration_items (base) + servers (derived) tabloları.
/// </summary>
[DynamicEntity("Server", MenuGroup = "CMDB")]
public sealed class ServerCI : ConfigurationItemBase
{
    public ServerType ServerType { get; private set; } = ServerType.Virtual;
    public DeploymentEnvironment Environment { get; private set; } = DeploymentEnvironment.Production;

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? OperatingSystem { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? IpAddress { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? Hostname { get; private set; }

    public int? CpuCores { get; private set; }
    public int? RamGB { get; private set; }
    public int? DiskGB { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? DataCenter { get; private set; }

    /// <summary>Sistem yöneticisi.</summary>
    public Guid? AdminUserId { get; private set; }

    private ServerCI() { }

    public static ServerCI Create(string name, string code, string? description = null,
        ServerType serverType = ServerType.Virtual,
        DeploymentEnvironment environment = DeploymentEnvironment.Production,
        CICriticality criticality = CICriticality.Medium,
        Guid? ownerUserId = null, Guid? adminUserId = null,
        string? operatingSystem = null, string? ipAddress = null,
        string? hostname = null, int? cpuCores = null, int? ramGB = null,
        int? diskGB = null, string? dataCenter = null)
    {
        return new ServerCI
        {
            Id = EntityId.New<ConfigurationItemId>(),
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            ServerType = serverType,
            Environment = environment,
            Criticality = criticality,
            OwnerUserId = ownerUserId,
            AdminUserId = adminUserId,
            OperatingSystem = operatingSystem,
            IpAddress = ipAddress,
            Hostname = hostname,
            CpuCores = cpuCores,
            RamGB = ramGB,
            DiskGB = diskGB,
            DataCenter = dataCenter
        };
    }

    public void Update(string? name = null, string? description = null,
        ServerType? serverType = null, DeploymentEnvironment? environment = null,
        CIStatus? status = null, CICriticality? criticality = null,
        Guid? ownerUserId = null, Guid? adminUserId = null,
        string? operatingSystem = null, string? ipAddress = null,
        string? hostname = null, int? cpuCores = null, int? ramGB = null,
        int? diskGB = null, string? dataCenter = null)
    {
        UpdateBase(name, description, status, criticality, ownerUserId);
        if (serverType.HasValue) ServerType = serverType.Value;
        if (environment.HasValue) Environment = environment.Value;
        if (adminUserId.HasValue) AdminUserId = adminUserId.Value;
        if (operatingSystem is not null) OperatingSystem = operatingSystem;
        if (ipAddress is not null) IpAddress = ipAddress;
        if (hostname is not null) Hostname = hostname;
        if (cpuCores.HasValue) CpuCores = cpuCores.Value;
        if (ramGB.HasValue) RamGB = ramGB.Value;
        if (diskGB.HasValue) DiskGB = diskGB.Value;
        if (dataCenter is not null) DataCenter = dataCenter;
    }
}
