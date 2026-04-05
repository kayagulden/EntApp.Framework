using EntApp.Modules.TaskManagement.Application.Commands;
using EntApp.Modules.TaskManagement.Application.Queries;
using EntApp.Modules.TaskManagement.Domain.Entities;
using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Modules.TaskManagement.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.TaskStatus;

namespace EntApp.Modules.TaskManagement.Infrastructure.Handlers;

// ── Queries ─────────────────────────────────────────────────
public sealed class ListProjectsQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListProjectsQuery, List<object>>
{
    public async Task<List<object>> Handle(ListProjectsQuery request, CancellationToken ct)
    {
        var query = db.Projects.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ProjectStatus>(request.Status, out var s))
            query = query.Where(p => p.Status == s);
        return await query.OrderBy(p => p.Name)
            .Select(p => (object)new { p.Id, p.Key, p.Name, Status = p.Status.ToString(),
                p.StartDate, p.EndDate, p.ManagerUserId, TaskCount = p.Tasks.Count })
            .ToListAsync(ct);
    }
}

public sealed class GetProjectQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetProjectQuery, object?>
{
    public async Task<object?> Handle(GetProjectQuery request, CancellationToken ct)
        => await db.Projects.FindAsync([request.Id], ct);
}

public sealed class ListTasksQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListTasksQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListTasksQuery request, CancellationToken ct)
    {
        var query = db.Tasks.Include(t => t.Project).AsQueryable();
        if (request.ProjectId.HasValue) query = query.Where(t => t.ProjectId.Value == request.ProjectId.Value);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TaskStatusEnum>(request.Status, out var s))
            query = query.Where(t => t.Status == s);
        if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<TaskPriority>(request.Priority, out var p))
            query = query.Where(t => t.Priority == p);
        if (Guid.TryParse(request.Assignee, out var uid))
            query = query.Where(t => t.AssigneeUserId == uid);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(t => (object)new { t.Id, t.TaskNumber, t.Title, ProjectKey = t.Project.Key,
                Status = t.Status.ToString(), Priority = t.Priority.ToString(),
                Type = t.Type.ToString(), t.AssigneeUserId, t.DueDate, t.EstimatedHours, t.SortOrder, t.ParentTaskId })
            .ToListAsync(ct);
        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetTaskQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetTaskQuery, object?>
{
    public async Task<object?> Handle(GetTaskQuery request, CancellationToken ct)
        => await db.Tasks.Include(x => x.SubTasks).Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id.Value == request.Id, ct);
}

public sealed class GetKanbanBoardQueryHandler(TaskManagementDbContext db) : IRequestHandler<GetKanbanBoardQuery, object>
{
    public async Task<object> Handle(GetKanbanBoardQuery request, CancellationToken ct)
    {
        var tasks = await db.Tasks.Where(t => t.ProjectId.Value == request.ProjectId)
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Id, t.TaskNumber, t.Title, Status = t.Status.ToString(),
                Priority = t.Priority.ToString(), Type = t.Type.ToString(),
                t.AssigneeUserId, t.SortOrder, t.DueDate })
            .ToListAsync(ct);
        return tasks.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.ToList());
    }
}

public sealed class ListCommentsQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListCommentsQuery, List<object>>
{
    public async Task<List<object>> Handle(ListCommentsQuery request, CancellationToken ct)
        => await db.Comments.Where(c => c.TaskId.Value == request.TaskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => (object)new { c.Id, c.AuthorUserId, c.Content, c.CreatedAt })
            .ToListAsync(ct);
}

public sealed class ListTimeEntriesQueryHandler(TaskManagementDbContext db) : IRequestHandler<ListTimeEntriesQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListTimeEntriesQuery request, CancellationToken ct)
    {
        var query = db.TimeEntries.AsQueryable();
        if (request.TaskId.HasValue) query = query.Where(t => t.TaskId.Value == request.TaskId.Value);
        if (request.UserId.HasValue) query = query.Where(t => t.UserId == request.UserId.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.WorkDate)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(t => (object)new { t.Id, t.TaskId, t.UserId, t.Hours, t.WorkDate, t.Description })
            .ToListAsync(ct);
        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

// ── Commands ────────────────────────────────────────────────
public sealed class CreateProjectCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateProjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        var project = ProjectBase.Create(request.Key, request.Name, request.Description,
            request.StartDate, request.EndDate, request.ManagerUserId);
        project.Activate();
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return project.Id.Value;
    }
}

public sealed class CreateTaskCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateTaskCommand, CreateTaskResult>
{
    public async Task<CreateTaskResult> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([request.ProjectId], ct)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found");
        Enum.TryParse<TaskType>(request.Type, out var type);
        Enum.TryParse<TaskPriority>(request.Priority, out var priority);
        var taskNumber = project.NextTaskNumber();
        var task = TaskItemBase.Create(project.Id, taskNumber, request.Title, type, priority,
            request.Description, request.AssigneeUserId, request.ReporterUserId,
            request.ParentTaskId.HasValue ? new TaskItemId(request.ParentTaskId.Value) : null,
            request.DueDate, request.EstimatedHours, request.Tags);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return new CreateTaskResult(task.Id.Value, task.TaskNumber);
    }
}

public sealed class MoveTaskCommandHandler(TaskManagementDbContext db) : IRequestHandler<MoveTaskCommand, MoveTaskResult>
{
    public async Task<MoveTaskResult> Handle(MoveTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([request.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");
        if (!Enum.TryParse<TaskStatusEnum>(request.Status, out var status))
            throw new ArgumentException($"Invalid status: {request.Status}");
        task.MoveTo(status);
        if (request.SortOrder.HasValue) task.SetSortOrder(request.SortOrder.Value);
        await db.SaveChangesAsync(ct);
        return new MoveTaskResult(task.Id.Value, task.Status.ToString(), task.SortOrder);
    }
}

public sealed class AssignTaskCommandHandler(TaskManagementDbContext db) : IRequestHandler<AssignTaskCommand, Guid>
{
    public async Task<Guid> Handle(AssignTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([request.TaskId], ct)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");
        task.AssignTo(request.UserId);
        await db.SaveChangesAsync(ct);
        return task.AssigneeUserId ?? Guid.Empty;
    }
}

public sealed class CreateCommentCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateCommentCommand, Guid>
{
    public async Task<Guid> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var comment = CommentBase.Create(new TaskItemId(request.TaskId), request.AuthorUserId, request.Content);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment.Id.Value;
    }
}

public sealed class CreateTimeEntryCommandHandler(TaskManagementDbContext db) : IRequestHandler<CreateTimeEntryCommand, Guid>
{
    public async Task<Guid> Handle(CreateTimeEntryCommand request, CancellationToken ct)
    {
        var entry = TimeEntryBase.Create(new TaskItemId(request.TaskId), request.UserId, request.Hours,
            request.WorkDate, request.Description);
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id.Value;
    }
}
