using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Modules.RequestManagement.Infrastructure.Persistence;
using EntApp.Modules.TaskManagement.Application.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.RequestManagement.Infrastructure.Handlers;

/// <summary>
/// Cross-module event handler: TaskManagement → RequestManagement.
/// TaskManagement'ın integration event'lerini dinleyerek ticket görev sayaçlarını günceller
/// ve tüm görevler tamamlandığında AllTasksDone ara durumuna geçirir.
/// </summary>
public sealed class TicketTaskCreatedEventHandler(
    RequestManagementDbContext db,
    ILogger<TicketTaskCreatedEventHandler> logger)
    : INotificationHandler<WorkItemCreatedForSourceEvent>
{
    public async Task Handle(WorkItemCreatedForSourceEvent notification, CancellationToken ct)
    {
        // Sadece RequestManagement.Ticket kaynağından gelen event'leri işle
        if (notification.SourceModule != "RequestManagement" || notification.SourceType != "Ticket")
            return;

        var ticket = await db.Tickets.FirstOrDefaultAsync(
            t => t.Id == new TicketId(notification.SourceId), ct);

        if (ticket is null)
        {
            logger.LogWarning("TicketTaskCreatedEventHandler: Ticket {SourceId} not found.", notification.SourceId);
            return;
        }

        ticket.IncrementTaskCount();
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {TicketNumber}: Yeni görev {WorkItemNumber} oluşturuldu. Toplam görev: {Count}",
            ticket.Number, notification.WorkItemNumber, ticket.LinkedTaskCount);
    }
}

public sealed class AllSourceWorkItemsCompletedEventHandler(
    RequestManagementDbContext db,
    ILogger<AllSourceWorkItemsCompletedEventHandler> logger)
    : INotificationHandler<AllSourceWorkItemsCompletedEvent>
{
    // Sistem kullanıcı ID — event-driven otomatik durum değişikliği için
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task Handle(AllSourceWorkItemsCompletedEvent notification, CancellationToken ct)
    {
        if (notification.SourceModule != "RequestManagement" || notification.SourceType != "Ticket")
            return;

        var ticket = await db.Tickets
            .Include(t => t.StatusHistory)
            .FirstOrDefaultAsync(t => t.Id == new TicketId(notification.SourceId), ct);

        if (ticket is null)
        {
            logger.LogWarning("AllSourceWorkItemsCompletedEventHandler: Ticket {SourceId} not found.", notification.SourceId);
            return;
        }

        ticket.SetTaskCounts(notification.CompletedWorkItemCount, notification.CompletedWorkItemCount);
        ticket.MarkAllTasksDone(SystemUserId);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {TicketNumber}: Tüm görevler tamamlandı ({Count}). Durum: AllTasksDone",
            ticket.Number, notification.CompletedWorkItemCount);
    }
}
