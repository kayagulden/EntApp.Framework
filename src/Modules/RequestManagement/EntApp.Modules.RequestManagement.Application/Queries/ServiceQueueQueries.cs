using MediatR;

namespace EntApp.Modules.RequestManagement.Application.Queries;

// ── DTOs ─────────────────────────────────────────────────────
public sealed record ServiceQueueDto(
    Guid Id, string Name, string Code, string? Description,
    Guid? DepartmentId, string? DepartmentName,
    Guid? ManagerUserId, Guid? DefaultWorkflowDefinitionId,
    bool IsActive, IReadOnlyList<QueueMemberDto> Members);

public sealed record QueueMemberDto(
    Guid Id, Guid UserId, string? UserName, string? FullName,
    string Role, DateTime JoinedAt, bool IsActive);

// ── Queries ──────────────────────────────────────────────────
public sealed record ListServiceQueuesQuery(
    Guid? DepartmentId = null, bool ActiveOnly = true) : IRequest<IReadOnlyList<ServiceQueueDto>>;

public sealed record GetServiceQueueQuery(Guid Id) : IRequest<ServiceQueueDto?>;
