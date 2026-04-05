using EntApp.Modules.TaskManagement.Application.Commands;
using EntApp.Modules.TaskManagement.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.TaskManagement.Infrastructure.Endpoints;

/// <summary>TaskManagement REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class TaskManagementEndpoints
{
    public static IEndpointRouteBuilder MapTaskManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var proj = app.MapGroup("/api/pm/projects").WithTags("PM - Projects");
        proj.MapGet("/", async (ISender mediator, string? status) => Results.Ok(await mediator.Send(new ListProjectsQuery(status)))).WithName("ListProjects");
        proj.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetProjectQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetProject");
        proj.MapPost("/", async (CreateProjectRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateProjectCommand(req.Key, req.Name, req.Description, req.StartDate, req.EndDate, req.ManagerUserId));
            return Results.Created($"/api/pm/projects/{id}", new { id });
        }).WithName("CreateProject");

        var tasks = app.MapGroup("/api/pm/tasks").WithTags("PM - Tasks");
        tasks.MapGet("/", async (ISender mediator, Guid? projectId, string? status, string? assignee, string? priority, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListTasksQuery(projectId, status, assignee, priority, page, pageSize)))).WithName("ListTasks");
        tasks.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetTaskQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetTask");
        tasks.MapPost("/", async (CreateTaskRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateTaskCommand(req.ProjectId, req.Title, req.Type, req.Priority,
                req.Description, req.AssigneeUserId, req.ReporterUserId, req.ParentTaskId, req.DueDate, req.EstimatedHours, req.Tags));
            return Results.Created($"/api/pm/tasks/{result.Id}", result);
        }).WithName("CreateTask");
        tasks.MapPost("/{id:guid}/move", async (Guid id, MoveTaskRequest req, ISender mediator) =>
            Results.Ok(await mediator.Send(new MoveTaskCommand(id, req.Status, req.SortOrder)))).WithName("MoveTask").WithSummary("Kanban sürükle-bırak");
        tasks.MapPost("/{id:guid}/assign", async (Guid id, AssignTaskRequest req, ISender mediator) =>
        {
            var userId = await mediator.Send(new AssignTaskCommand(id, req.UserId));
            return Results.Ok(new { id, assigneeUserId = userId });
        }).WithName("AssignTask");
        tasks.MapGet("/board/{projectId:guid}", async (Guid projectId, ISender mediator)
            => Results.Ok(await mediator.Send(new GetKanbanBoardQuery(projectId)))).WithName("KanbanBoard").WithSummary("Kanban board — duruma göre gruplu");

        var comments = app.MapGroup("/api/pm/comments").WithTags("PM - Comments");
        comments.MapGet("/{taskId:guid}", async (Guid taskId, ISender mediator)
            => Results.Ok(await mediator.Send(new ListCommentsQuery(taskId)))).WithName("ListComments");
        comments.MapPost("/", async (CreateCommentRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateCommentCommand(req.TaskId, req.AuthorUserId, req.Content));
            return Results.Created($"/api/pm/comments/{id}", new { id });
        }).WithName("CreateComment");

        var time = app.MapGroup("/api/pm/time-entries").WithTags("PM - Time Entries");
        time.MapGet("/", async (ISender mediator, Guid? taskId, Guid? userId, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListTimeEntriesQuery(taskId, userId, page, pageSize)))).WithName("ListTimeEntries");
        time.MapPost("/", async (CreateTimeEntryRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateTimeEntryCommand(req.TaskId, req.UserId, req.Hours, req.WorkDate, req.Description));
            return Results.Created($"/api/pm/time-entries/{id}", new { id });
        }).WithName("CreateTimeEntry");

        return app;
    }
}

// ── Request DTO'lar ─────────────────────────────────────────
public sealed record CreateProjectRequest(string Key, string Name, string? Description = null,
    DateTime? StartDate = null, DateTime? EndDate = null, Guid? ManagerUserId = null);
public sealed record CreateTaskRequest(Guid ProjectId, string Title, string Type = "Task",
    string Priority = "Medium", string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, Guid? ParentTaskId = null, DateTime? DueDate = null,
    decimal EstimatedHours = 0, string? Tags = null);
public sealed record MoveTaskRequest(string Status, int? SortOrder = null);
public sealed record AssignTaskRequest(Guid UserId);
public sealed record CreateCommentRequest(Guid TaskId, Guid AuthorUserId, string Content);
public sealed record CreateTimeEntryRequest(Guid TaskId, Guid UserId, decimal Hours,
    DateTime WorkDate, string? Description = null);
