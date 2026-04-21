using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Board kolonu — projeye bağlı, özelleştirilebilir Kanban kolonları.
/// Her kolon bir WorkItemStatus'a map eder. Kart sürüklenince work item'ın Status'ı güncellenir.
/// </summary>
public sealed class BoardColumn : AuditableEntity<BoardColumnId>, ITenantEntity
{
    public ProjectId ProjectId { get; private set; }

    /// <summary>Kolon adı: "Yapılacak", "Geliştirme", "QA Test", ...</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Kolon sırası (0-based).</summary>
    public int Order { get; private set; }

    /// <summary>Work-in-Progress limiti. Aşılırsa UI uyarı gösterir.</summary>
    public int? WipLimit { get; private set; }

    /// <summary>Bu kolona düşen work item'ların WorkItemStatus değeri.</summary>
    public WorkItemStatus MappedStatus { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProjectBase? Project { get; private set; }

    private BoardColumn() { }

    public static BoardColumn Create(ProjectId projectId, string name,
        int order, WorkItemStatus mappedStatus, int? wipLimit = null)
    {
        return new BoardColumn
        {
            Id = EntityId.New<BoardColumnId>(),
            ProjectId = projectId,
            Name = name,
            Order = order,
            MappedStatus = mappedStatus,
            WipLimit = wipLimit
        };
    }

    public void Update(string? name = null, int? order = null,
        int? wipLimit = null, WorkItemStatus? mappedStatus = null)
    {
        if (name is not null) Name = name;
        if (order.HasValue) Order = order.Value;
        if (wipLimit.HasValue) WipLimit = wipLimit.Value;
        if (mappedStatus.HasValue) MappedStatus = mappedStatus.Value;
    }

    public void SetOrder(int order) => Order = order;

    /// <summary>Proje oluşturulduğunda varsayılan kolonları döner.</summary>
    public static List<BoardColumn> CreateDefaults(ProjectId projectId)
    {
        return
        [
            Create(projectId, "Bekleyenler", 0, WorkItemStatus.Backlog),
            Create(projectId, "Yapılacak", 1, WorkItemStatus.Todo),
            Create(projectId, "İşlemde", 2, WorkItemStatus.InProgress),
            Create(projectId, "İnceleme", 3, WorkItemStatus.InReview),
            Create(projectId, "Tamamlandı", 4, WorkItemStatus.Done),
            Create(projectId, "İptal", 5, WorkItemStatus.Cancelled),
        ];
    }
}
