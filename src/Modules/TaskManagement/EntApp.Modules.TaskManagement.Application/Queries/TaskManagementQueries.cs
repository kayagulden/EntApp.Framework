using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.TaskManagement.Application.Queries;

public sealed record ListProjectsQuery(string? Status) : IRequest<List<object>>;
public sealed record GetProjectQuery(Guid Id) : IRequest<object?>;
public sealed record ListTasksQuery(Guid? ProjectId, string? Status, string? Assignee, string? Priority,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;
public sealed record GetTaskQuery(Guid Id) : IRequest<object?>;
public sealed record GetKanbanBoardQuery(Guid ProjectId) : IRequest<object>;
public sealed record ListCommentsQuery(Guid TaskId) : IRequest<List<object>>;
public sealed record ListTimeEntriesQuery(Guid? TaskId, Guid? UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;
