using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>
/// Hizmet Kuyruğu — taleplerin düştüğü ve yetkili üyelerin claim edebildiği kuyruk.
/// Departmandan bağımsız tanımlanabilir, opsiyonel DepartmentId ile ilişkilendirilebilir.
/// </summary>
public sealed class ServiceQueue : AggregateRoot<ServiceQueueId>, ITenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Opsiyonel departman bağlantısı. null ise cross-departman kuyruk.</summary>
    public DepartmentId? DepartmentId { get; private set; }

    /// <summary>Kuyruk sorumlusu — departman yöneticisinden farklı olabilir.</summary>
    public Guid? ManagerUserId { get; private set; }

    /// <summary>Bu kuyruğa düşen taleplerde varsayılan olarak kullanılacak Elsa workflow.</summary>
    public Guid? DefaultWorkflowDefinitionId { get; private set; }

    public bool IsActive { get; private set; } = true;
    public Guid TenantId { get; set; }

    // Navigation
    public Department? Department { get; private set; }
    public ICollection<QueueMembership> Members { get; private set; } = [];

    private ServiceQueue() { }

    public static ServiceQueue Create(string name, string code, string? description,
        DepartmentId? departmentId, Guid? managerUserId, Guid? defaultWorkflowDefinitionId)
    {
        return new ServiceQueue
        {
            Id = EntityId.New<ServiceQueueId>(),
            Name = name,
            Code = code,
            Description = description,
            DepartmentId = departmentId,
            ManagerUserId = managerUserId,
            DefaultWorkflowDefinitionId = defaultWorkflowDefinitionId
        };
    }

    public void Update(string name, string code, string? description,
        DepartmentId? departmentId, Guid? managerUserId, Guid? defaultWorkflowDefinitionId)
    {
        Name = name;
        Code = code;
        Description = description;
        DepartmentId = departmentId;
        ManagerUserId = managerUserId;
        DefaultWorkflowDefinitionId = defaultWorkflowDefinitionId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
