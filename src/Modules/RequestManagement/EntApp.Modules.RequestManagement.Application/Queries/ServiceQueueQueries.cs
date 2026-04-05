using EntApp.Modules.RequestManagement.Domain.Entities;
using MediatR;

namespace EntApp.Modules.RequestManagement.Application.Queries;

// ── ServiceQueue Queries ─────────────────────────────────────
public sealed record ListServiceQueuesQuery(
    Guid? DepartmentId = null, bool ActiveOnly = true) : IRequest<IReadOnlyList<ServiceQueue>>;

public sealed record GetServiceQueueQuery(Guid Id) : IRequest<ServiceQueue?>;
