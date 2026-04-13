using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.TaskManagement.Application.Queries;

public sealed record ListProjectsQuery(string? Status) : IRequest<List<object>>;
public sealed record GetProjectQuery(Guid Id) : IRequest<object?>;
public sealed record ListTasksQuery(Guid? ProjectId, string? Status, string? Assignee, string? Priority,
    int Page = 1, int PageSize = 20, Guid? ReporterUserId = null, string? AssigneeUserIds = null,
    string? Type = null, string? SourceFilter = null) : IRequest<PagedResult<object>>;
public sealed record GetTaskQuery(Guid Id) : IRequest<TaskDetailDto?>;
public sealed record GetKanbanBoardQuery(Guid ProjectId) : IRequest<object>;
public sealed record ListCommentsQuery(Guid TaskId) : IRequest<List<object>>;
public sealed record ListTimeEntriesQuery(Guid? TaskId, Guid? UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;

/// <summary>Belirli bir kaynağa (Ticket vb.) bağlı görevleri listeler.</summary>
public sealed record ListTasksBySourceQuery(
    string SourceModule, string SourceType, Guid SourceId
) : IRequest<List<TaskBySourceDto>>;

public sealed record TaskBySourceDto(
    Guid Id, string TaskNumber, string Title, string Status,
    string Priority, string Type, Guid? AssigneeUserId,
    DateTime? DueDate, decimal EstimatedHours, DateTime CreatedAt);

public sealed record TaskDetailDto(
    Guid Id, string TaskNumber, string Title, string? Description,
    string Status, string Priority, string Type,
    Guid? AssigneeUserId, Guid? ReporterUserId,
    Guid? ParentTaskId, DateTime? DueDate, decimal EstimatedHours,
    int SortOrder, string? Tags,
    string? SourceModule, string? SourceType, Guid? SourceId,
    Guid? ProjectId, string? ProjectKey, string? ProjectName,
    DateTime CreatedAt, DateTime? UpdatedAt,
    List<TaskBySourceDto> SubTasks);
