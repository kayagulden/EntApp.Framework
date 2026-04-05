using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Enums;
using MediatR;

namespace EntApp.Modules.RequestManagement.Application.Queries;

// ── Department ───────────────────────────────────────────────
public sealed record ListDepartmentsQuery(bool? ActiveOnly = true) : IRequest<IReadOnlyList<Department>>;
public sealed record GetDepartmentQuery(Guid Id) : IRequest<Department?>;

// ── RequestCategory ──────────────────────────────────────────
public sealed record ListCategoriesQuery(Guid? DepartmentId = null, bool? ActiveOnly = true) : IRequest<IReadOnlyList<RequestCategory>>;
public sealed record GetCategoryQuery(Guid Id) : IRequest<RequestCategory?>;

// ── SlaDefinition ────────────────────────────────────────────
public sealed record ListSlaDefinitionsQuery(bool? ActiveOnly = true) : IRequest<IReadOnlyList<SlaDefinition>>;

// ── Ticket ───────────────────────────────────────────────────
public sealed record ListTicketsQuery(
    TicketStatus? Status, TicketPriority? Priority,
    Guid? AssigneeUserId, Guid? DepartmentId,
    int Page = 1, int PageSize = 20) : IRequest<TicketListResult>;

public sealed record GetTicketQuery(Guid Id) : IRequest<Ticket?>;

public sealed record GetMyTicketsQuery(Guid ReporterUserId, int Page = 1, int PageSize = 20)
    : IRequest<TicketListResult>;

// ── Result Types ─────────────────────────────────────────────
public sealed record TicketListResult(IReadOnlyList<Ticket> Items, int TotalCount);
