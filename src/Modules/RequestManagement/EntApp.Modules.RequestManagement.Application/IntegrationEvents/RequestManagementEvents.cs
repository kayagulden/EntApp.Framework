using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.RequestManagement.Application.IntegrationEvents;

/// <summary>Yeni ticket oluşturulduğunda → Notification, SLA sayacı, Workflow başlatma.</summary>
public sealed record TicketCreatedEvent(
    Guid TicketId, string TicketNumber, string Title,
    Guid CategoryId, Guid DepartmentId,
    Guid ReporterUserId, string Priority, string Channel) : IntegrationEvent;

/// <summary>Ticket atandığında → Notification.</summary>
public sealed record TicketAssignedEvent(
    Guid TicketId, string TicketNumber,
    Guid AssigneeUserId, Guid? PreviousAssigneeUserId) : IntegrationEvent;

/// <summary>SLA ihlal edildiğinde → Notification (eskalasyon), Reporting.</summary>
public sealed record TicketSlaBreachedEvent(
    Guid TicketId, string TicketNumber,
    string BreachType, DateTime Deadline) : IntegrationEvent;

/// <summary>Ticket çözüldüğünde → Notification (talep sahibine).</summary>
public sealed record TicketResolvedEvent(
    Guid TicketId, string TicketNumber,
    Guid ReporterUserId, Guid? AssigneeUserId,
    DateTime ResolvedAt) : IntegrationEvent;
