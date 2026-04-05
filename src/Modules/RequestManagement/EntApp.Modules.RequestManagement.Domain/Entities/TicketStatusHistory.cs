using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.RequestManagement.Domain.Entities;

/// <summary>Talep durum geçmişi — her status değişikliği kayıt altına alınır.</summary>
public sealed class TicketStatusHistory : BaseEntity<TicketStatusHistoryId>
{
    public TicketId TicketId { get; private set; }
    public TicketStatus OldStatus { get; private set; }
    public TicketStatus NewStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? Reason { get; private set; }

    // Navigation
    public Ticket Ticket { get; private set; } = null!;

    private TicketStatusHistory() { }

    public static TicketStatusHistory Create(TicketId ticketId, TicketStatus oldStatus, TicketStatus newStatus,
        Guid changedByUserId, string? reason = null)
    {
        return new TicketStatusHistory
        {
            Id = EntityId.New<TicketStatusHistoryId>(),
            TicketId = ticketId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };
    }
}
