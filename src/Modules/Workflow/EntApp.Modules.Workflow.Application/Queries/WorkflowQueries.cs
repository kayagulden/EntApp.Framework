using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.Workflow.Application.Queries;

public sealed record ListDefinitionsQuery(string? Category = null,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<DefinitionListItem>>;
public sealed record DefinitionListItem(Guid Id, string Name, string Title,
    string? Description, string? Category, string ApprovalType,
    int? TimeoutHours, bool IsActive, DateTime CreatedAt);

public sealed record GetDefinitionQuery(Guid Id) : IRequest<object?>;

public sealed record ListWorkflowsQuery(string? Status = null,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<WorkflowListItem>>;
public sealed record WorkflowListItem(Guid Id, string DefinitionName,
    string Status, string? ReferenceType, string? ReferenceId,
    int CurrentStepOrder, DateTime? StartedAt, DateTime? CompletedAt, DateTime CreatedAt);

public sealed record GetWorkflowQuery(Guid Id) : IRequest<object?>;

public sealed record GetPendingStepsQuery(Guid UserId) : IRequest<IReadOnlyList<PendingStepItem>>;
public sealed record PendingStepItem(Guid Id, Guid InstanceId, int StepOrder,
    string StepName, Guid? AssignedUserId, DateTime? DueDate, DateTime CreatedAt);
