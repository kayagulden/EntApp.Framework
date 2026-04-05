using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Proje.</summary>
[DynamicEntity("Project", MenuGroup = "Proje Yönetimi")]
public sealed class ProjectBase : AuditableEntity<ProjectId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 10, Searchable = true)]
    public string Key { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 2000)]
    public string? Description { get; private set; }

    public ProjectStatus Status { get; private set; } = ProjectStatus.Planning;

    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    /// <summary>Proje yöneticisi</summary>
    public Guid? ManagerUserId { get; private set; }

    /// <summary>Otomatik görev numaralandırma sayacı</summary>
    public int TaskSequence { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<TaskItemBase> Tasks { get; private set; } = [];

    private ProjectBase() { }

    public static ProjectBase Create(string key, string name, string? description = null,
        DateTime? startDate = null, DateTime? endDate = null, Guid? managerUserId = null)
    {
        return new ProjectBase
        {
            Id = EntityId.New<ProjectId>(), Key = key.ToUpperInvariant(), Name = name,
            Description = description, StartDate = startDate, EndDate = endDate,
            ManagerUserId = managerUserId
        };
    }

    public void Activate() => Status = ProjectStatus.Active;
    public void Complete() => Status = ProjectStatus.Completed;

    /// <summary>Yeni görev numarası üretir: KEY-1, KEY-2, ...</summary>
    public string NextTaskNumber()
    {
        TaskSequence++;
        return $"{Key}-{TaskSequence}";
    }
}
