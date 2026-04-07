using EntApp.Shared.Kernel.Domain.Attributes;
using EntApp.Shared.Kernel.Domain.Ids;

namespace EntApp.Shared.Kernel.Domain.Entities;

/// <summary>
/// Departman — organizasyona bağlı, hiyerarşik departman yapısı.
/// Shared Kernel referans verisi — IAM ve RequestManagement dahil tüm modüller tarafından kullanılır.
/// </summary>
[DynamicEntity("Department", MenuGroup = "Organization")]
public sealed class Department : AuditableEntity<DepartmentId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string Code { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Description { get; private set; }

    /// <summary>Bağlı organizasyon.</summary>
    public OrganizationId? OrganizationId { get; private set; }

    /// <summary>Departman yöneticisi (IAM User ID).</summary>
    public Guid? ManagerUserId { get; private set; }

    /// <summary>Üst departman — hiyerarşik yapı için.</summary>
    public DepartmentId? ParentDepartmentId { get; private set; }

    /// <summary>Bu departmana gelen taleplerin otomatik yönlendirileceği varsayılan kuyruk (Guid, modül-agnostik).</summary>
    public Guid? DefaultQueueId { get; private set; }

    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public Organization? Organization { get; private set; }
    public Department? ParentDepartment { get; private set; }
    public ICollection<Department> SubDepartments { get; private set; } = [];

    private Department() { }

    public static Department Create(string name, string code, string? description = null,
        OrganizationId? organizationId = null, Guid? managerUserId = null,
        DepartmentId? parentDepartmentId = null, Guid? defaultQueueId = null)
    {
        return new Department
        {
            Id = EntityId.New<DepartmentId>(),
            Name = name,
            Code = code,
            Description = description,
            OrganizationId = organizationId,
            ManagerUserId = managerUserId,
            ParentDepartmentId = parentDepartmentId,
            DefaultQueueId = defaultQueueId
        };
    }

    public void Update(string name, string code, string? description, Guid? managerUserId,
        DepartmentId? parentId, OrganizationId? organizationId = null, Guid? defaultQueueId = null)
    {
        Name = name;
        Code = code;
        Description = description;
        ManagerUserId = managerUserId;
        ParentDepartmentId = parentId;
        OrganizationId = organizationId;
        DefaultQueueId = defaultQueueId;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
