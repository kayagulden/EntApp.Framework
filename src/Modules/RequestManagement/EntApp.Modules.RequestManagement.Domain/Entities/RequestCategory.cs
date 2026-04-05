using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>Talep kategorisi — departman, SLA, workflow bağlantıları.</summary>
[DynamicEntity("RequestCategory", MenuGroup = "Request Management")]
public sealed class RequestCategory : AuditableEntity<RequestCategoryId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string Code { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Description { get; private set; }

    public DepartmentId DepartmentId { get; private set; }
    public SlaDefinitionId? SlaDefinitionId { get; private set; }

    /// <summary>Workflow definition ID (nullable) — kategori için otomatik başlatılan workflow.</summary>
    public Guid? WorkflowDefinitionId { get; private set; }

    /// <summary>Dinamik form şeması (JSON). Talep oluşturma formunda render edilir.</summary>
    public string? FormSchemaJson { get; private set; }

    /// <summary>Efor eşiği — bu değerin üzerinde talepler otomatik proje adayı olur.</summary>
    public int? AutoProjectThreshold { get; private set; }

    [DynamicField(FieldType = FieldType.Boolean)]
    public bool IsActive { get; private set; } = true;

    public Guid TenantId { get; set; }

    // Navigation
    public Department Department { get; private set; } = null!;
    public SlaDefinition? SlaDefinitionEntity { get; private set; }
    public ICollection<Ticket> Tickets { get; private set; } = [];

    private RequestCategory() { }

    public static RequestCategory Create(string name, string code, DepartmentId departmentId,
        string? description = null, SlaDefinitionId? slaDefinitionId = null,
        Guid? workflowDefinitionId = null, string? formSchemaJson = null,
        int? autoProjectThreshold = null)
    {
        return new RequestCategory
        {
            Id = EntityId.New<RequestCategoryId>(),
            Name = name,
            Code = code,
            DepartmentId = departmentId,
            Description = description,
            SlaDefinitionId = slaDefinitionId,
            WorkflowDefinitionId = workflowDefinitionId,
            FormSchemaJson = formSchemaJson,
            AutoProjectThreshold = autoProjectThreshold
        };
    }

    public void Update(string name, string code, DepartmentId departmentId,
        string? description, SlaDefinitionId? slaDefinitionId,
        Guid? workflowDefinitionId, string? formSchemaJson, int? autoProjectThreshold)
    {
        Name = name;
        Code = code;
        DepartmentId = departmentId;
        Description = description;
        SlaDefinitionId = slaDefinitionId;
        WorkflowDefinitionId = workflowDefinitionId;
        FormSchemaJson = formSchemaJson;
        AutoProjectThreshold = autoProjectThreshold;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
