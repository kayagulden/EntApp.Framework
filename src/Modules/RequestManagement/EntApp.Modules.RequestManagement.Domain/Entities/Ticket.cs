using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Entities;
using EntApp.Shared.Kernel.Domain.Ids;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>StateFlow state name sabitleri — flow designer'daki Name alanlarıyla eşleşir.</summary>
public static class TicketStates
{
    public const string New = "New";
    public const string WaitForAssignment = "WaitForAssignment";
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string WaitingForInfo = "WaitingForInfo";
    public const string Escalated = "Escalated";
    public const string AllTasksDone = "AllTasksDone";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
}

/// <summary>Talep (Ticket) — AggregateRoot, domain events desteği.</summary>
public sealed class Ticket : AggregateRoot<TicketId>, ITenantEntity
{
    /// <summary>Otomatik artan, okunabilir talep numarası (REQ-0001).</summary>
    public string Number { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TicketPriority Priority { get; private set; } = TicketPriority.Medium;
    public string Status { get; private set; } = TicketStates.New;
    public TicketChannel Channel { get; private set; } = TicketChannel.Portal;

    // SLA
    public DateTime? SlaResponseDeadline { get; private set; }
    public DateTime? SlaResolutionDeadline { get; private set; }
    public bool SlaResponseBreached { get; private set; }
    public bool SlaResolutionBreached { get; private set; }

    // Kullanıcılar
    public Guid ReporterUserId { get; private set; }
    public Guid? AssigneeUserId { get; private set; }

    // İlişkiler
    public RequestCategoryId CategoryId { get; private set; }
    public DepartmentId DepartmentId { get; private set; }
    public ServiceQueueId? ServiceQueueId { get; private set; }
    public TicketRoutingSource RoutingSource { get; private set; } = TicketRoutingSource.Unrouted;
    public string? WorkflowInstanceId { get; private set; }
    public Guid? ProjectId { get; private set; }

    /// <summary>Bu ticket'ın bağlı olduğu StateFlow tanımının ID'si.</summary>
    public Guid? FlowDefinitionId { get; private set; }

    /// <summary>İlgili Configuration Item (uygulama, sunucu vb.) — cross-module, raw Guid.</summary>
    public Guid? ConfigurationItemId { get; private set; }

    // Görev entegrasyonu (denormalized — event-driven güncellenir)
    /// <summary>Bu talebe bağlı toplam görev sayısı.</summary>
    public int LinkedTaskCount { get; private set; }
    /// <summary>Bu talebe bağlı tamamlanmış görev sayısı.</summary>
    public int CompletedTaskCount { get; private set; }

    /// <summary>Dinamik form verileri (JSON). RequestCategory.FormSchemaJson'a göre doldurulan alan değerleri.</summary>
    public string? FormDataJson { get; private set; }

    // Zaman damgaları
    public DateTime? FirstResponseAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public RequestCategory Category { get; private set; } = null!;
    public Department Department { get; private set; } = null!;
    public ServiceQueue? ServiceQueue { get; private set; }
    public ICollection<TicketComment> Comments { get; private set; } = [];
    public ICollection<TicketStatusHistory> StatusHistory { get; private set; } = [];

    private Ticket() { }

    public static Ticket Create(string number, string title, RequestCategoryId categoryId,
        DepartmentId departmentId, Guid reporterUserId,
        string? description = null, TicketPriority priority = TicketPriority.Medium,
        TicketChannel channel = TicketChannel.Portal,
        string? formDataJson = null,
        ServiceQueueId? serviceQueueId = null,
        TicketRoutingSource routingSource = TicketRoutingSource.Unrouted,
        Guid? configurationItemId = null)
    {
        return new Ticket
        {
            Id = EntityId.New<TicketId>(),
            Number = number,
            Title = title,
            Description = description,
            Priority = priority,
            Channel = channel,
            CategoryId = categoryId,
            DepartmentId = departmentId,
            ReporterUserId = reporterUserId,
            FormDataJson = formDataJson,
            ServiceQueueId = serviceQueueId,
            RoutingSource = routingSource,
            ConfigurationItemId = configurationItemId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetFormData(string? formDataJson) => FormDataJson = formDataJson;

    /// <summary>İlgili CI'ı ayarlar veya kaldırır.</summary>
    public void SetConfigurationItem(Guid? configurationItemId) => ConfigurationItemId = configurationItemId;

    public void SetSlaDeadlines(DateTime? responseDeadline, DateTime? resolutionDeadline)
    {
        SlaResponseDeadline = responseDeadline;
        SlaResolutionDeadline = resolutionDeadline;
    }

    public void Assign(Guid userId)
    {
        AssigneeUserId = userId;
    }

    /// <summary>Ticket'ı havuza geri bırakır — assignee kaldırılır.</summary>
    public void Unassign()
    {
        AssigneeUserId = null;
    }

    public void ChangeStatus(string newStatus, Guid changedByUserId, string? reason = null)
    {
        var old = Status;
        Status = newStatus;

        StatusHistory.Add(TicketStatusHistory.Create(
            Id, old, newStatus, changedByUserId, reason));

        if (newStatus == TicketStates.Resolved) ResolvedAt = DateTime.UtcNow;
        if (newStatus == TicketStates.Closed) ClosedAt = DateTime.UtcNow;
    }

    public void RecordFirstResponse()
    {
        if (FirstResponseAt is null) FirstResponseAt = DateTime.UtcNow;
    }

    public void BreachSlaResponse() => SlaResponseBreached = true;
    public void BreachSlaResolution() => SlaResolutionBreached = true;

    public void LinkWorkflow(string workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;
    public void LinkProject(Guid projectId) => ProjectId = projectId;

    /// <summary>Ticket'ı belirtilen StateFlow tanımına bağlar.</summary>
    public void SetFlowDefinition(Guid flowDefinitionId) => FlowDefinitionId = flowDefinitionId;

    /// <summary>Ticket'ı belirtilen queue'ya yönlendirir.</summary>
    public void RouteToQueue(ServiceQueueId queueId, TicketRoutingSource source)
    {
        ServiceQueueId = queueId;
        RoutingSource = source;
    }

    /// <summary>Ticket'ın queue atamasını kaldırır (unrouted durumuna döner).</summary>
    public void UnrouteFromQueue()
    {
        ServiceQueueId = null;
        RoutingSource = TicketRoutingSource.Unrouted;
    }

    public void Update(string title, string? description, TicketPriority priority,
        Guid? configurationItemId = null)
    {
        Title = title;
        Description = description;
        Priority = priority;
        if (configurationItemId.HasValue) ConfigurationItemId = configurationItemId;
    }

    // ── Görev Entegrasyonu ──────────────────────────────────
    /// <summary>Yeni bir görev oluşturulduğunda çağrılır (event handler tarafından).</summary>
    public void IncrementTaskCount() => LinkedTaskCount++;

    /// <summary>Görev sayaçlarını günceller (toplam ve tamamlanan).</summary>
    public void SetTaskCounts(int total, int completed)
    {
        LinkedTaskCount = total;
        CompletedTaskCount = completed;
    }

    /// <summary>Tüm görevler tamamlandığında çağrılır — AllTasksDone ara durumuna geçer.</summary>
    public void MarkAllTasksDone(Guid systemUserId)
    {
        if (Status is TicketStates.InProgress or TicketStates.Open or TicketStates.New)
        {
            var old = Status;
            Status = TicketStates.AllTasksDone;
            StatusHistory.Add(TicketStatusHistory.Create(Id, old, TicketStates.AllTasksDone, systemUserId,
                "Tüm bağlı görevler tamamlandı."));
        }
    }
}
