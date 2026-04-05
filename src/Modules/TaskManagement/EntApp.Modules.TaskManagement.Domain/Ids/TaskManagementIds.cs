using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Ids;

public readonly record struct ProjectId(Guid Value) : IEntityId;
public readonly record struct TaskItemId(Guid Value) : IEntityId;
public readonly record struct CommentId(Guid Value) : IEntityId;
public readonly record struct TimeEntryId(Guid Value) : IEntityId;
