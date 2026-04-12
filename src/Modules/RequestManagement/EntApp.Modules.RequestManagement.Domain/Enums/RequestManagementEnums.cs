namespace EntApp.Modules.RequestManagement.Domain.Enums;

public enum TicketStatus
{
    New = 0,
    Open = 1,
    InProgress = 2,
    WaitingForInfo = 3,
    Escalated = 4,
    Resolved = 5,
    Closed = 6,
    Cancelled = 7,
    Reopened = 8,
    /// <summary>Tüm bağlı görevler tamamlandı, son onay bekleniyor.</summary>
    AllTasksDone = 9
}

public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
    Urgent = 4
}

public enum TicketChannel
{
    Portal = 0,
    Email = 1,
    Phone = 2,
    Chat = 3,
    Internal = 4
}

/// <summary>Ticket'ın queue'ya nasıl yönlendirildiğini belirtir (audit amaçlı).</summary>
public enum TicketRoutingSource
{
    /// <summary>Kullanıcı açıkça bir queue seçti.</summary>
    Manual = 0,
    /// <summary>RequestCategory'nin default queue'sundan otomatik atandı.</summary>
    CategoryDefault = 1,
    /// <summary>Department'ın default queue'sundan otomatik atandı.</summary>
    DepartmentDefault = 2,
    /// <summary>Elsa workflow kuralı ile atandı.</summary>
    WorkflowRule = 3,
    /// <summary>Hiçbir routing kuralı eşleşmedi — dispatcher ataması bekleniyor.</summary>
    Unrouted = 4
}
