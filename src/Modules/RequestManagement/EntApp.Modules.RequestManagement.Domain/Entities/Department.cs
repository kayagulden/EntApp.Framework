using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>Departman — talep yönlendirme ve organizasyon yapısı.</summary>
[DynamicEntity("Department", MenuGroup = "Request Management")]
public sealed class Department : AuditableEntity<DepartmentId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string Code { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Description { get; private set; }

    public Guid? ManagerUserId { get; private set; }
    public DepartmentId? ParentDepartmentId { get; private set; }

    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public Department? ParentDepartment { get; private set; }
    public ICollection<Department> SubDepartments { get; private set; } = [];
    public ICollection<RequestCategory> Categories { get; private set; } = [];

    private Department() { }

    public static Department Create(string name, string code, string? description = null,
        Guid? managerUserId = null, DepartmentId? parentDepartmentId = null)
    {
        return new Department
        {
            Id = EntityId.New<DepartmentId>(),
            Name = name,
            Code = code,
            Description = description,
            ManagerUserId = managerUserId,
            ParentDepartmentId = parentDepartmentId
        };
    }

    public void Update(string name, string code, string? description, Guid? managerUserId, DepartmentId? parentId)
    {
        Name = name;
        Code = code;
        Description = description;
        ManagerUserId = managerUserId;
        ParentDepartmentId = parentId;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
