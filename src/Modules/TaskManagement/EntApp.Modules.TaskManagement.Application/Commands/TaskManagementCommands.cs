using MediatR;

namespace EntApp.Modules.TaskManagement.Application.Commands;

public sealed record CreateProjectCommand(string Key, string Name, string? Description = null,
    DateTime? StartDate = null, DateTime? EndDate = null, Guid? ManagerUserId = null) : IRequest<Guid>;

public sealed record CreateTaskCommand(Guid ProjectId, string Title, string Type = "Task",
    string Priority = "Medium", string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, Guid? ParentTaskId = null, DateTime? DueDate = null,
    decimal EstimatedHours = 0, string? Tags = null) : IRequest<CreateTaskResult>;
public sealed record CreateTaskResult(Guid Id, string TaskNumber);

public sealed record MoveTaskCommand(Guid TaskId, string Status, int? SortOrder = null) : IRequest<MoveTaskResult>;
public sealed record MoveTaskResult(Guid Id, string Status, int SortOrder);

public sealed record AssignTaskCommand(Guid TaskId, Guid UserId) : IRequest<Guid>;
public sealed record CreateCommentCommand(Guid TaskId, Guid AuthorUserId, string Content) : IRequest<Guid>;
public sealed record CreateTimeEntryCommand(Guid TaskId, Guid UserId, decimal Hours,
    DateTime WorkDate, string? Description = null) : IRequest<Guid>;
