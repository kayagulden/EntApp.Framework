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
        // ── Portfolio ──────────────────────────────────────
        var portfolios = app.MapGroup("/api/pm/portfolios").WithTags("PM - Portfolios");
        portfolios.MapGet("/", async (ISender mediator, string? status) =>
            Results.Ok(await mediator.Send(new ListPortfoliosQuery(status)))).WithName("ListPortfolios");
        portfolios.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetPortfolioQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetPortfolio");
        portfolios.MapPost("/", async (CreatePortfolioRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreatePortfolioCommand(req.Name, req.Code, req.Description, req.OwnerUserId));
            return Results.Created($"/api/pm/portfolios/{id}", new { id });
        }).WithName("CreatePortfolio");
        portfolios.MapPut("/{id:guid}", async (Guid id, UpdatePortfolioRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdatePortfolioCommand(id, req.Name, req.Code, req.Description, req.OwnerUserId, req.Status));
            return Results.Ok(new { id });
        }).WithName("UpdatePortfolio");

        // ── Application ───────────────────────────────────────
        var apps = app.MapGroup("/api/pm/applications").WithTags("PM - Applications");
        apps.MapGet("/", async (ISender mediator, string? status, string? applicationType) =>
            Results.Ok(await mediator.Send(new ListApplicationsQuery(status, applicationType)))).WithName("ListApplications");
        apps.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetApplicationQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetApplication");
        apps.MapPost("/", async (CreateApplicationRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateApplicationCommand(req.Name, req.Code, req.Description,
                req.ApplicationType ?? "InHouse", req.Criticality ?? "Medium",
                req.OwnerUserId, req.TechLeadUserId, req.TechnologyStack,
                req.RepositoryUrl, req.DocumentationUrl, req.CurrentVersion));
            return Results.Created($"/api/pm/applications/{id}", new { id });
        }).WithName("CreateApplication");
        apps.MapPut("/{id:guid}", async (Guid id, UpdateApplicationRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateApplicationCommand(id, req.Name, req.Description,
                req.ApplicationType, req.Status, req.Criticality,
                req.OwnerUserId, req.TechLeadUserId, req.TechnologyStack,
                req.RepositoryUrl, req.DocumentationUrl, req.CurrentVersion));
            return Results.Ok(new { id });
        }).WithName("UpdateApplication");

        // ── Project ──────────────────────────────────────────
        var proj = app.MapGroup("/api/pm/projects").WithTags("PM - Projects");
        proj.MapGet("/", async (ISender mediator, string? status, Guid? portfolioId) =>
            Results.Ok(await mediator.Send(new ListProjectsQuery(status, portfolioId)))).WithName("ListProjects");
        proj.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetProjectQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetProject");
        proj.MapPost("/", async (CreateProjectRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateProjectCommand(req.Key, req.Name, req.Description,
                req.StartDate, req.EndDate, req.TargetEndDate,
                req.ManagerUserId, req.OwnerUserId, req.PortfolioId,
                req.Methodology ?? "Kanban", req.Category ?? "General"));
            return Results.Created($"/api/pm/projects/{id}", new { id });
        }).WithName("CreateProject");
        proj.MapPut("/{id:guid}", async (Guid id, UpdateProjectRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateProjectCommand(id, req.Name, req.Description,
                req.StartDate, req.EndDate, req.TargetEndDate,
                req.ManagerUserId, req.OwnerUserId, req.PortfolioId, req.Status, req.Methodology,
                req.Category));
            return Results.Ok(new { id });
        }).WithName("UpdateProject");
        var tasks = app.MapGroup("/api/pm/tasks").WithTags("PM - Tasks");
        tasks.MapGet("/", async (ISender mediator, Guid? projectId, string? status, string? assignee, string? priority,
            int page = 1, int pageSize = 20, Guid? reporterUserId = null, string? assigneeUserIds = null,
            string? type = null, string? sourceFilter = null)
            => Results.Ok(await mediator.Send(new ListTasksQuery(projectId, status, assignee, priority, page, pageSize,
                reporterUserId, assigneeUserIds, type, sourceFilter)))).WithName("ListTasks");
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
        tasks.MapPut("/{id:guid}", async (Guid id, UpdateTaskRequest req, ISender mediator) =>
        {
            var taskId = await mediator.Send(new UpdateTaskCommand(id, req.Title, req.Description,
                req.Priority, req.Type, req.DueDate, req.EstimatedHours, req.Tags, req.AssigneeUserId));
            return Results.Ok(new { id = taskId });
        }).WithName("UpdateTask").WithSummary("Görev bilgilerini günceller");
        tasks.MapGet("/board/{projectId:guid}", async (Guid projectId, ISender mediator)
            => Results.Ok(await mediator.Send(new GetKanbanBoardQuery(projectId)))).WithName("KanbanBoard").WithSummary("Kanban board — duruma göre gruplu");

        // ── Cross-Module Source Endpoints ────────────────────────
        tasks.MapGet("/by-source", async (ISender mediator, string module, string type, Guid sourceId)
            => Results.Ok(await mediator.Send(new ListTasksBySourceQuery(module, type, sourceId))))
            .WithName("ListTasksBySource").WithSummary("Kaynağa bağlı görevleri listeler (Ticket vb.)");

        tasks.MapPost("/from-source", async (CreateTaskFromSourceRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateTaskFromSourceCommand(
                req.SourceModule, req.SourceType, req.SourceId,
                req.Title, req.Description, req.AssigneeUserId, req.ReporterUserId,
                req.Priority, req.DueDate, req.ProjectId));
            return Results.Created($"/api/pm/tasks/{result.Id}", result);
        }).WithName("CreateTaskFromSource").WithSummary("Dış kaynaktan görev oluşturur (Ticket → Task)");

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
public sealed record CreatePortfolioRequest(string Name, string Code,
    string? Description = null, Guid? OwnerUserId = null);
public sealed record UpdatePortfolioRequest(string? Name = null, string? Code = null,
    string? Description = null, Guid? OwnerUserId = null, string? Status = null);
public sealed record CreateApplicationRequest(string Name, string Code,
    string? Description = null, string? ApplicationType = null,
    string? Criticality = null, Guid? OwnerUserId = null,
    Guid? TechLeadUserId = null, string? TechnologyStack = null,
    string? RepositoryUrl = null, string? DocumentationUrl = null,
    string? CurrentVersion = null);
public sealed record UpdateApplicationRequest(string? Name = null,
    string? Description = null, string? ApplicationType = null,
    string? Status = null, string? Criticality = null,
    Guid? OwnerUserId = null, Guid? TechLeadUserId = null,
    string? TechnologyStack = null, string? RepositoryUrl = null,
    string? DocumentationUrl = null, string? CurrentVersion = null);
public sealed record CreateProjectRequest(string Key, string Name, string? Description = null,
    DateTime? StartDate = null, DateTime? EndDate = null, DateTime? TargetEndDate = null,
    Guid? ManagerUserId = null, Guid? OwnerUserId = null,
    Guid? PortfolioId = null, string? Methodology = null, string? Category = null);
public sealed record UpdateProjectRequest(string? Name = null, string? Description = null,
    DateTime? StartDate = null, DateTime? EndDate = null, DateTime? TargetEndDate = null,
    Guid? ManagerUserId = null, Guid? OwnerUserId = null,
    Guid? PortfolioId = null, string? Status = null, string? Methodology = null,
    string? Category = null);
public sealed record CreateTaskRequest(Guid? ProjectId, string Title, string Type = "Task",
    string Priority = "Medium", string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, Guid? ParentTaskId = null, DateTime? DueDate = null,
    decimal EstimatedHours = 0, string? Tags = null);
public sealed record CreateTaskFromSourceRequest(
    string SourceModule, string SourceType, Guid SourceId,
    string Title, string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, string Priority = "Medium",
    DateTime? DueDate = null, Guid? ProjectId = null);
public sealed record MoveTaskRequest(string Status, int? SortOrder = null);
public sealed record AssignTaskRequest(Guid? UserId);
public sealed record CreateCommentRequest(Guid TaskId, Guid AuthorUserId, string Content);
public sealed record CreateTimeEntryRequest(Guid TaskId, Guid UserId, decimal Hours,
    DateTime WorkDate, string? Description = null);
public sealed record UpdateTaskRequest(string? Title = null, string? Description = null,
    string? Priority = null, string? Type = null, DateTime? DueDate = null,
    decimal? EstimatedHours = null, string? Tags = null, Guid? AssigneeUserId = null);
