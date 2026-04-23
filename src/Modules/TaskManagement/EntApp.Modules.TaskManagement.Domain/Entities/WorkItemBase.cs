using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;
using WorkItemStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.WorkItemStatus;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>Görev / iş kalemi.</summary>
[DynamicEntity("TaskItem", MenuGroup = "Proje Yönetimi")]
public sealed class WorkItemBase : AuditableEntity<WorkItemId>, ITenantEntity
{
    /// <summary>Opsiyonel proje bağlantısı. null ise proje dışı görev (talep görevi, bağımsız görev).</summary>
    public ProjectId? ProjectId { get; private set; }

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 20, Searchable = true)]
    public string WorkItemNumber { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 500, Searchable = true)]
    public string Title { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 5000)]
    public string? Description { get; private set; }

    public WorkItemStatusEnum Status { get; private set; } = WorkItemStatusEnum.Backlog;
    public WorkItemPriority Priority { get; private set; } = WorkItemPriority.Medium;
    public WorkItemType Type { get; private set; } = WorkItemType.Task;

    /// <summary>Atanan kişi</summary>
    public Guid? AssigneeUserId { get; private set; }

    /// <summary>Raporlayan kişi</summary>
    public Guid? ReporterUserId { get; private set; }

    /// <summary>Üst görev (alt görev desteği)</summary>
    public WorkItemId? ParentTaskId { get; private set; }

    public DateTime? DueDate { get; private set; }

    /// <summary>Tahmini süre (saat)</summary>
    public decimal EstimatedHours { get; private set; }

    /// <summary>Kanban sıralama</summary>
    public int SortOrder { get; private set; }

    /// <summary>Etiketler (virgülle ayrılmış)</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 500)]
    public string? Tags { get; private set; }

    // ── Cross-Module Kaynak Referansı ────────────────────────
    /// <summary>Görevi tetikleyen kaynak modül. null ise bağımsız görev. Örn: "RequestManagement"</summary>
    public string? SourceModule { get; private set; }

    /// <summary>Kaynak entity tipi. Örn: "Ticket"</summary>
    public string? SourceType { get; private set; }

    /// <summary>Kaynak entity ID. Örn: Ticket.Id</summary>
    public Guid? SourceId { get; private set; }

    // ── Work Item Hierarchy & Sprint ─────────────────────────
    /// <summary>Story Points — Scrum estimation (0, 1, 2, 3, 5, 8, 13, 21).</summary>
    public int? StoryPoints { get; private set; }

    /// <summary>Kabul kriterleri (Markdown) — UserStory/Feature için.</summary>
    public string? AcceptanceCriteria { get; private set; }

    /// <summary>Sprint bağlantısı (nullable — sprintsiz çalışma mümkün).</summary>
    public SprintId? SprintId { get; private set; }

    /// <summary>Milestone bağlantısı (nullable). Bu iş kalemi hangi milestone'a katkı sağlıyor?</summary>
    public MilestoneId? MilestoneId { get; private set; }

    // ── WSJF (Weighted Shortest Job First) ────────────────────
    /// <summary>İş değeri (1-13 Fibonacci). WSJF Cost of Delay bileşeni.</summary>
    public int? BusinessValue { get; private set; }

    /// <summary>Zaman hassasiyeti (1-13 Fibonacci). WSJF Cost of Delay bileşeni.</summary>
    public int? TimeCriticality { get; private set; }

    /// <summary>Risk azaltma / fırsat yaratma (1-13 Fibonacci). WSJF Cost of Delay bileşeni.</summary>
    public int? RiskReduction { get; private set; }

    /// <summary>Hesaplanmış WSJF skoru: (BV + TC + RR) / JobSize.</summary>
    public decimal? WsjfScore { get; private set; }

    /// <summary>Hiyerarşi derinliği cache'i — 0:Epic, 1:Feature, 2:Story, 3:Task.</summary>
    public int HierarchyLevel { get; private set; }

    // ── Kanban Metrikleri Timestamp'leri ──────────────────────
    /// <summary>İlk kez InProgress'e geçtiği an (Cycle Time başlangıcı).</summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>Done/Cancelled'a geçtiği an (Lead Time & Cycle Time bitişi).</summary>
    public DateTime? CompletedAt { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProjectBase? Project { get; private set; }
    public WorkItemBase? ParentTask { get; private set; }
    public SprintBase? Sprint { get; private set; }
    public MilestoneBase? Milestone { get; private set; }
    public ICollection<WorkItemBase> SubTasks { get; private set; } = [];
    public ICollection<CommentBase> Comments { get; private set; } = [];
    public ICollection<TimeEntryBase> TimeEntries { get; private set; } = [];

    private WorkItemBase() { }

    /// <summary>Proje kapsamında görev oluşturur.</summary>
    public static WorkItemBase Create(ProjectId projectId, string taskNumber, string title,
        WorkItemType type = WorkItemType.Task, WorkItemPriority priority = WorkItemPriority.Medium,
        string? description = null, Guid? assigneeUserId = null,
        Guid? reporterUserId = null, WorkItemId? parentTaskId = null,
        DateTime? dueDate = null, decimal estimatedHours = 0, string? tags = null)
    {
        return new WorkItemBase
        {
            Id = EntityId.New<WorkItemId>(), ProjectId = projectId, WorkItemNumber = taskNumber,
            Title = title, Type = type, Priority = priority,
            Description = description, AssigneeUserId = assigneeUserId,
            ReporterUserId = reporterUserId, ParentTaskId = parentTaskId,
            DueDate = dueDate, EstimatedHours = estimatedHours, Tags = tags
        };
    }

    /// <summary>Dış kaynaktan (Ticket vb.) iş kalemi oluşturur. Tip ve parent desteği ile.</summary>
    public static WorkItemBase CreateFromSource(
        string sourceModule, string sourceType, Guid sourceId,
        string taskNumber, string title,
        WorkItemType type = WorkItemType.Task, WorkItemPriority priority = WorkItemPriority.Medium,
        string? description = null, Guid? assigneeUserId = null,
        Guid? reporterUserId = null, DateTime? dueDate = null,
        decimal estimatedHours = 0, ProjectId? projectId = null,
        WorkItemId? parentTaskId = null, int hierarchyLevel = 0)
    {
        return new WorkItemBase
        {
            Id = EntityId.New<WorkItemId>(),
            ProjectId = projectId,
            WorkItemNumber = taskNumber,
            Title = title,
            Type = type,
            Priority = priority,
            Description = description,
            AssigneeUserId = assigneeUserId,
            ReporterUserId = reporterUserId,
            DueDate = dueDate,
            EstimatedHours = estimatedHours,
            SourceModule = sourceModule,
            SourceType = sourceType,
            SourceId = sourceId,
            ParentTaskId = parentTaskId,
            HierarchyLevel = hierarchyLevel
        };
    }

    /// <summary>Bağımsız (projesiz, kaynaksız) görev oluşturur.</summary>
    public static WorkItemBase CreateStandalone(
        string taskNumber, string title,
        WorkItemType type = WorkItemType.Task, WorkItemPriority priority = WorkItemPriority.Medium,
        string? description = null, Guid? assigneeUserId = null,
        Guid? reporterUserId = null, DateTime? dueDate = null,
        decimal estimatedHours = 0, string? tags = null)
    {
        return new WorkItemBase
        {
            Id = EntityId.New<WorkItemId>(),
            WorkItemNumber = taskNumber,
            Title = title,
            Type = type,
            Priority = priority,
            Description = description,
            AssigneeUserId = assigneeUserId,
            ReporterUserId = reporterUserId,
            DueDate = dueDate,
            EstimatedHours = estimatedHours,
            Tags = tags
        };
    }

    public void MoveTo(WorkItemStatusEnum status)
    {
        // Cycle Time: ilk kez "işe başlama" durumuna geçtiğinde StartedAt set edilir
        if (StartedAt is null && status == WorkItemStatusEnum.InProgress)
            StartedAt = DateTime.UtcNow;

        // Lead Time / Cycle Time bitişi: terminal duruma geçtiğinde
        if (CompletedAt is null && status is WorkItemStatusEnum.Done or WorkItemStatusEnum.Cancelled)
            CompletedAt = DateTime.UtcNow;

        // Eğer terminal durumdan geri açılırsa CompletedAt sıfırlanır
        if (CompletedAt.HasValue && status is not (WorkItemStatusEnum.Done or WorkItemStatusEnum.Cancelled))
            CompletedAt = null;

        Status = status;
    }
    public void AssignTo(Guid userId) => AssigneeUserId = userId;
    public void Unassign() => AssigneeUserId = null;
    public void SetSortOrder(int order) => SortOrder = order;
    public void SetStoryPoints(int? points)
    {
        StoryPoints = points;
        RecalculateWsjf();
    }
    public void SetAcceptanceCriteria(string? criteria) => AcceptanceCriteria = criteria;
    public void AssignToSprint(SprintId? sprintId) => SprintId = sprintId;
    public void SetHierarchyLevel(int level) => HierarchyLevel = level;
    public void MoveToProject(ProjectId projectId) => ProjectId = projectId;
    public void SetParent(WorkItemId? parentId, int hierarchyLevel) { ParentTaskId = parentId; HierarchyLevel = hierarchyLevel; }

    /// <summary>WSJF bileşenlerini ayarlar ve skoru otomatik hesaplar.</summary>
    public void SetWsjfComponents(int? businessValue, int? timeCriticality, int? riskReduction)
    {
        BusinessValue = businessValue;
        TimeCriticality = timeCriticality;
        RiskReduction = riskReduction;
        RecalculateWsjf();
    }

    private void RecalculateWsjf()
    {
        if (BusinessValue.HasValue && TimeCriticality.HasValue && RiskReduction.HasValue
            && StoryPoints.HasValue && StoryPoints.Value > 0)
            WsjfScore = (decimal)(BusinessValue.Value + TimeCriticality.Value + RiskReduction.Value) / StoryPoints.Value;
        else
            WsjfScore = null;
    }

    /// <summary>Görev bilgilerini günceller.</summary>
    public void Update(string? title = null, string? description = null,
        WorkItemPriority? priority = null, WorkItemType? type = null,
        DateTime? dueDate = null, decimal? estimatedHours = null, string? tags = null,
        int? storyPoints = null, string? acceptanceCriteria = null)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (priority.HasValue) Priority = priority.Value;
        if (type.HasValue) Type = type.Value;
        if (dueDate.HasValue) DueDate = dueDate.Value;
        if (estimatedHours.HasValue) EstimatedHours = estimatedHours.Value;
        if (tags is not null) Tags = tags;
        if (storyPoints.HasValue) SetStoryPoints(storyPoints.Value);
        if (acceptanceCriteria is not null) AcceptanceCriteria = acceptanceCriteria;
    }

    /// <summary>Görevin bir kaynağa bağlı olup olmadığını kontrol eder.</summary>
    public bool HasSource => SourceId.HasValue;

    /// <summary>Harcanan toplam süre</summary>
    public decimal TotalLoggedHours => TimeEntries.Sum(t => t.Hours);
}
