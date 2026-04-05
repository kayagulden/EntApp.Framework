using EntApp.Modules.TaskManagement.Domain.Entities;
using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Modules.TaskManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TaskStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.TaskStatus;

namespace EntApp.Modules.TaskManagement.Infrastructure.Endpoints;

/// <summary>TaskManagement REST API endpoint'leri.</summary>
public static class TaskManagementEndpoints
{
    public static IEndpointRouteBuilder MapTaskManagementEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Projects ═══════════
        var proj = app.MapGroup("/api/pm/projects").WithTags("PM - Projects");

        proj.MapGet("/", async (TaskManagementDbContext db, string? status) =>
        {
            var query = db.Projects.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ProjectStatus>(status, out var s))
                query = query.Where(p => p.Status == s);

            var items = await query.OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Key, p.Name, Status = p.Status.ToString(),
                    p.StartDate, p.EndDate, p.ManagerUserId, TaskCount = p.Tasks.Count })
                .ToListAsync();
            return Results.Ok(items);
        }).WithName("ListProjects");

        proj.MapGet("/{id:guid}", async (Guid id, TaskManagementDbContext db) =>
        {
            var p = await db.Projects.FindAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        }).WithName("GetProject");

        proj.MapPost("/", async (CreateProjectRequest req, TaskManagementDbContext db) =>
        {
            var project = ProjectBase.Create(req.Key, req.Name, req.Description,
                req.StartDate, req.EndDate, req.ManagerUserId);
            project.Activate();
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return Results.Created($"/api/pm/projects/{project.Id}",
                new { project.Id, project.Key });
        }).WithName("CreateProject");

        // ═══════════ Tasks ═══════════
        var tasks = app.MapGroup("/api/pm/tasks").WithTags("PM - Tasks");

        tasks.MapGet("/", async (TaskManagementDbContext db, Guid? projectId, string? status,
            string? assignee, string? priority, int page = 1, int pageSize = 20) =>
        {
            var query = db.Tasks.Include(t => t.Project).AsQueryable();
            if (projectId.HasValue) query = query.Where(t => t.ProjectId.Value == projectId.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskStatusEnum>(status, out var s))
                query = query.Where(t => t.Status == s);
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var p))
                query = query.Where(t => t.Priority == p);
            if (Guid.TryParse(assignee, out var uid))
                query = query.Where(t => t.AssigneeUserId == uid);

            var total = await query.CountAsync();
            var items = await query.OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => new { t.Id, t.TaskNumber, t.Title,
                    ProjectKey = t.Project.Key,
                    Status = t.Status.ToString(), Priority = t.Priority.ToString(),
                    Type = t.Type.ToString(), t.AssigneeUserId, t.DueDate,
                    t.EstimatedHours, t.SortOrder, t.ParentTaskId })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListTasks");

        tasks.MapGet("/{id:guid}", async (Guid id, TaskManagementDbContext db) =>
        {
            var t = await db.Tasks.Include(x => x.SubTasks).Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Id.Value == id);
            return t is null ? Results.NotFound() : Results.Ok(t);
        }).WithName("GetTask");

        tasks.MapPost("/", async (CreateTaskRequest req, TaskManagementDbContext db) =>
        {
            var project = await db.Projects.FindAsync(req.ProjectId);
            if (project is null) return Results.BadRequest(new { error = "Project not found." });

            Enum.TryParse<TaskType>(req.Type, out var type);
            Enum.TryParse<TaskPriority>(req.Priority, out var priority);
            var taskNumber = project.NextTaskNumber();

            var task = TaskItemBase.Create(project.Id, taskNumber, req.Title, type, priority,
                req.Description, req.AssigneeUserId, req.ReporterUserId, req.ParentTaskId.HasValue ? new TaskItemId(req.ParentTaskId.Value) : null,
                req.DueDate, req.EstimatedHours, req.Tags);
            db.Tasks.Add(task);
            await db.SaveChangesAsync();

            return Results.Created($"/api/pm/tasks/{task.Id}",
                new { task.Id, task.TaskNumber });
        }).WithName("CreateTask");

        // ── Kanban move ──────────────────────────────────
        tasks.MapPost("/{id:guid}/move", async (Guid id, MoveTaskRequest req, TaskManagementDbContext db) =>
        {
            var task = await db.Tasks.FindAsync(id);
            if (task is null) return Results.NotFound();
            if (!Enum.TryParse<TaskStatusEnum>(req.Status, out var status))
                return Results.BadRequest(new { error = "Invalid status." });
            task.MoveTo(status);
            if (req.SortOrder.HasValue) task.SetSortOrder(req.SortOrder.Value);
            await db.SaveChangesAsync();
            return Results.Ok(new { task.Id, Status = task.Status.ToString(), task.SortOrder });
        }).WithName("MoveTask").WithSummary("Kanban sürükle-bırak");

        tasks.MapPost("/{id:guid}/assign", async (Guid id, AssignTaskRequest req, TaskManagementDbContext db) =>
        {
            var task = await db.Tasks.FindAsync(id);
            if (task is null) return Results.NotFound();
            task.AssignTo(req.UserId);
            await db.SaveChangesAsync();
            return Results.Ok(new { task.Id, task.AssigneeUserId });
        }).WithName("AssignTask");

        // ── Kanban board ─────────────────────────────────
        tasks.MapGet("/board/{projectId:guid}", async (Guid projectId, TaskManagementDbContext db) =>
        {
            var tasks2 = await db.Tasks.Where(t => t.ProjectId.Value == projectId)
                .OrderBy(t => t.SortOrder)
                .Select(t => new { t.Id, t.TaskNumber, t.Title,
                    Status = t.Status.ToString(), Priority = t.Priority.ToString(),
                    Type = t.Type.ToString(), t.AssigneeUserId, t.SortOrder, t.DueDate })
                .ToListAsync();

            var board = tasks2.GroupBy(t => t.Status)
                .ToDictionary(g => g.Key, g => g.ToList());

            return Results.Ok(board);
        }).WithName("KanbanBoard").WithSummary("Kanban board — duruma göre gruplu");

        // ═══════════ Comments ═══════════
        var comments = app.MapGroup("/api/pm/comments").WithTags("PM - Comments");

        comments.MapGet("/{taskId:guid}", async (Guid taskId, TaskManagementDbContext db) =>
        {
            var items = await db.Comments.Where(c => c.TaskId.Value == taskId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new { c.Id, c.AuthorUserId, c.Content, c.CreatedAt })
                .ToListAsync();
            return Results.Ok(items);
        }).WithName("ListComments");

        comments.MapPost("/", async (CreateCommentRequest req, TaskManagementDbContext db) =>
        {
            var comment = CommentBase.Create(new TaskItemId(req.TaskId), req.AuthorUserId, req.Content);
            db.Comments.Add(comment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/pm/comments/{comment.Id}", new { comment.Id });
        }).WithName("CreateComment");

        // ═══════════ Time Entries ═══════════
        var time = app.MapGroup("/api/pm/time-entries").WithTags("PM - Time Entries");

        time.MapGet("/", async (TaskManagementDbContext db, Guid? taskId, Guid? userId,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.TimeEntries.AsQueryable();
            if (taskId.HasValue) query = query.Where(t => t.TaskId.Value == taskId.Value);
            if (userId.HasValue) query = query.Where(t => t.UserId == userId.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(t => t.WorkDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => new { t.Id, t.TaskId, t.UserId, t.Hours, t.WorkDate, t.Description })
                .ToListAsync();
            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListTimeEntries");

        time.MapPost("/", async (CreateTimeEntryRequest req, TaskManagementDbContext db) =>
        {
            var entry = TimeEntryBase.Create(new TaskItemId(req.TaskId), req.UserId, req.Hours,
                req.WorkDate, req.Description);
            db.TimeEntries.Add(entry);
            await db.SaveChangesAsync();
            return Results.Created($"/api/pm/time-entries/{entry.Id}",
                new { entry.Id, entry.Hours });
        }).WithName("CreateTimeEntry");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
public sealed record CreateProjectRequest(string Key, string Name, string? Description = null,
    DateTime? StartDate = null, DateTime? EndDate = null, Guid? ManagerUserId = null);

public sealed record CreateTaskRequest(Guid ProjectId, string Title,
    string Type = "Task", string Priority = "Medium", string? Description = null,
    Guid? AssigneeUserId = null, Guid? ReporterUserId = null, Guid? ParentTaskId = null,
    DateTime? DueDate = null, decimal EstimatedHours = 0, string? Tags = null);

public sealed record MoveTaskRequest(string Status, int? SortOrder = null);
public sealed record AssignTaskRequest(Guid UserId);

public sealed record CreateCommentRequest(Guid TaskId, Guid AuthorUserId, string Content);
public sealed record CreateTimeEntryRequest(Guid TaskId, Guid UserId, decimal Hours,
    DateTime WorkDate, string? Description = null);
