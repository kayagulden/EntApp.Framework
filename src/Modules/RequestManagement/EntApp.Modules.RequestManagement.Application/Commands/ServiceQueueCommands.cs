using MediatR;

namespace EntApp.Modules.RequestManagement.Application.Commands;

// ── ServiceQueue Commands ────────────────────────────────────
public sealed record CreateServiceQueueCommand(
    string Name, string Code, string? Description,
    Guid? DepartmentId, Guid? ManagerUserId,
    Guid? DefaultWorkflowDefinitionId) : IRequest<Guid>;

public sealed record UpdateServiceQueueCommand(
    Guid Id, string Name, string Code, string? Description,
    Guid? DepartmentId, Guid? ManagerUserId,
    Guid? DefaultWorkflowDefinitionId) : IRequest;

// ── QueueMembership Commands ─────────────────────────────────
public sealed record AddQueueMemberCommand(
    Guid QueueId, Guid UserId, string Role = "Member") : IRequest<Guid>;

public sealed record RemoveQueueMemberCommand(Guid MembershipId) : IRequest;

public sealed record UpdateQueueMemberRoleCommand(
    Guid MembershipId, string Role) : IRequest;
