using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Enums;
using MediatR;

namespace EntApp.Modules.RequestManagement.Application.Queries;

// ── RequestCategory ──────────────────────────────────────────
public sealed record ListCategoriesQuery(Guid? DepartmentId = null, bool? ActiveOnly = true) : IRequest<IReadOnlyList<RequestCategory>>;
public sealed record GetCategoryQuery(Guid Id) : IRequest<RequestCategory?>;

// ── SlaDefinition ────────────────────────────────────────────
public sealed record ListSlaDefinitionsQuery(bool? ActiveOnly = true) : IRequest<IReadOnlyList<SlaDefinition>>;

// ── Ticket ───────────────────────────────────────────────────
public sealed record ListTicketsQuery(
    TicketStatus? Status, TicketPriority? Priority,
    Guid? AssigneeUserId, Guid? ReporterUserId, Guid? DepartmentId,
    Guid? ServiceQueueId = null,
    string? QueueIds = null,
    bool UnassignedOnly = false,
    Guid? ConfigurationItemId = null,
    int Page = 1, int PageSize = 20) : IRequest<TicketListResult>;

public sealed record GetTicketQuery(Guid Id) : IRequest<Ticket?>;

public sealed record GetMyTicketsQuery(Guid ReporterUserId, int Page = 1, int PageSize = 20)
    : IRequest<TicketListResult>;

// ── Queue ────────────────────────────────────────────────────
/// <summary>Belirtilen kullanıcının üyesi olduğu kuyrukları döndürür (rolle birlikte).</summary>
public sealed record GetMyQueuesQuery(Guid UserId) : IRequest<IReadOnlyList<MyQueueDto>>;

/// <summary>Belirtilen kuyruğun üyelerini döndürür (görev atama için).</summary>
public sealed record ListQueueMembersQuery(Guid QueueId) : IRequest<IReadOnlyList<TaskAssigneeDto>>;

// ── Result Types ─────────────────────────────────────────────
public sealed record TicketListResult(IReadOnlyList<Ticket> Items, int TotalCount);

public sealed record MyQueueDto(
    Guid QueueId, string Name, string Code, string? Description,
    string? DepartmentName, string Role, int TicketCount, int UnassignedCount);

public sealed record TaskAssigneeDto(Guid UserId, string Role, string? DisplayName);
