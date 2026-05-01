namespace EntApp.Modules.StateFlow.Domain.Enums;

/// <summary>Event-driven otomasyon kuralları için tetikleyici tipleri.</summary>
public enum TriggerType
{
    /// <summary>SLA yanıt süresi aşıldı.</summary>
    SLAResponseBreached = 0,
    /// <summary>SLA çözüm süresi aşıldı.</summary>
    SLAResolutionBreached = 1,
    /// <summary>Ticket belirli süre atanmadan/güncellenmeden bekledi.</summary>
    TicketIdleTimeout = 2,
    /// <summary>Öncelik değişti.</summary>
    PriorityChanged = 3,
    /// <summary>Atama değişti.</summary>
    AssignmentChanged = 4,
    /// <summary>Entity oluşturuldu.</summary>
    EntityCreated = 5,
    /// <summary>Entity güncellendi.</summary>
    EntityUpdated = 6,
    /// <summary>Yorum eklendi.</summary>
    CommentAdded = 7
}
