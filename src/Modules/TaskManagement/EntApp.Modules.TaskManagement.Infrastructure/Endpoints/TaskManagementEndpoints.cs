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
        apps.MapGet("/{id:guid}/projects", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListCIProjectsQuery(id)))).WithName("ListApplicationProjects");

        // ── Project ────────────────────────────────────────────
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

        // ── ProjectDeliverable (Proje ↔ CI) ─────────────────
        proj.MapGet("/{id:guid}/deliverables", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListProjectDeliverablesQuery(id))))
            .WithName("ListProjectDeliverables");
        proj.MapPost("/{id:guid}/deliverables", async (Guid id, AddDeliverableRequest req, ISender mediator) =>
        {
            var deliverableId = await mediator.Send(new AddProjectDeliverableCommand(
                id, req.ConfigurationItemId, req.Role ?? "Primary", req.Notes));
            return Results.Created($"/api/pm/projects/{id}/deliverables", new { id = deliverableId });
        }).WithName("AddProjectDeliverable");
        proj.MapDelete("/{id:guid}/deliverables/{ciId:guid}", async (Guid id, Guid ciId, ISender mediator) =>
        {
            await mediator.Send(new RemoveProjectDeliverableCommand(id, ciId));
            return Results.NoContent();
        }).WithName("RemoveProjectDeliverable");

        // ── Sprint (proje altında) ─────────────────────
        proj.MapGet("/{projectId:guid}/sprints", async (Guid projectId, ISender mediator, string? status) =>
            Results.Ok(await mediator.Send(new ListSprintsQuery(projectId, status))))
            .WithName("ListProjectSprints").WithTags("PM - Sprints");
        proj.MapPost("/{projectId:guid}/sprints", async (Guid projectId, CreateSprintRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateSprintCommand(projectId, req.Name,
                req.StartDate, req.EndDate, req.Goal, req.CapacityPoints));
            return Results.Created($"/api/pm/sprints/{id}", new { id });
        }).WithName("CreateSprint").WithTags("PM - Sprints");

        // ── Backlog ───────────────────────────────────
        proj.MapGet("/{projectId:guid}/backlog", async (Guid projectId, ISender mediator,
            string view = "flat", string? type = null, string? sprint = null,
            Guid? assignee = null, string? status = null) =>
            Results.Ok(await mediator.Send(new GetBacklogQuery(projectId, view, type, sprint, assignee, status))))
            .WithName("GetProjectBacklog").WithTags("PM - Backlog")
            .WithSummary("Proje backlog — flat veya tree görünüm");

        // ── Sprint (bağımsız endpoint'ler) ───────────────
        var sprints = app.MapGroup("/api/pm/sprints").WithTags("PM - Sprints");
        sprints.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetSprintQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetSprint");
        sprints.MapPut("/{id:guid}", async (Guid id, UpdateSprintRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateSprintCommand(id, req.Name, req.Goal, req.StartDate, req.EndDate, req.CapacityPoints));
            return Results.Ok(new { id });
        }).WithName("UpdateSprint");
        sprints.MapPost("/{id:guid}/start", async (Guid id, ISender mediator) =>
            Results.Ok(new { id = await mediator.Send(new StartSprintCommand(id)) }))
            .WithName("StartSprint").WithSummary("Sprint başlat");
        sprints.MapPost("/{id:guid}/complete", async (Guid id, ISender mediator) =>
            Results.Ok(new { id = await mediator.Send(new CompleteSprintCommand(id)) }))
            .WithName("CompleteSprint").WithSummary("Sprint tamamla");

        // ── Server ──────────────────────────────────────────────
        var servers = app.MapGroup("/api/pm/servers").WithTags("PM - Servers");
        servers.MapGet("/", async (ISender mediator, string? status, string? serverType, string? environment) =>
            Results.Ok(await mediator.Send(new ListServersQuery(status, serverType, environment)))).WithName("ListServers");
        servers.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetServerQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetServer");
        servers.MapPost("/", async (CreateServerRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateServerCommand(req.Name, req.Code, req.Description,
                req.ServerType ?? "Virtual", req.Environment ?? "Production", req.Criticality ?? "Medium",
                req.OwnerUserId, req.AdminUserId, req.OperatingSystem, req.IpAddress,
                req.Hostname, req.CpuCores, req.RamGB, req.DiskGB, req.DataCenter));
            return Results.Created($"/api/pm/servers/{id}", new { id });
        }).WithName("CreateServer");
        servers.MapPut("/{id:guid}", async (Guid id, UpdateServerRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateServerCommand(id, req.Name, req.Description,
                req.ServerType, req.Environment, req.Status, req.Criticality,
                req.OwnerUserId, req.AdminUserId, req.OperatingSystem, req.IpAddress,
                req.Hostname, req.CpuCores, req.RamGB, req.DiskGB, req.DataCenter));
            return Results.Ok(new { id });
        }).WithName("UpdateServer");
        servers.MapGet("/{id:guid}/projects", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListCIProjectsQuery(id)))).WithName("ListServerProjects");

        // ── Database ────────────────────────────────────────────
        var databases = app.MapGroup("/api/pm/databases").WithTags("PM - Databases");
        databases.MapGet("/", async (ISender mediator, string? status, string? databaseEngine) =>
            Results.Ok(await mediator.Send(new ListDatabasesQuery(status, databaseEngine)))).WithName("ListDatabases");
        databases.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetDatabaseQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetDatabase");
        databases.MapPost("/", async (CreateDatabaseRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateDatabaseCommand(req.Name, req.Code, req.Description,
                req.DatabaseEngine ?? "PostgreSQL", req.Criticality ?? "Medium",
                req.OwnerUserId, req.AdminUserId, req.Version, req.Port, req.SizeGB,
                req.ConnectionString, req.BackupSchedule));
            return Results.Created($"/api/pm/databases/{id}", new { id });
        }).WithName("CreateDatabase");
        databases.MapPut("/{id:guid}", async (Guid id, UpdateDatabaseRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateDatabaseCommand(id, req.Name, req.Description,
                req.DatabaseEngine, req.Status, req.Criticality,
                req.OwnerUserId, req.AdminUserId, req.Version, req.Port, req.SizeGB,
                req.ConnectionString, req.BackupSchedule));
            return Results.Ok(new { id });
        }).WithName("UpdateDatabase");
        databases.MapGet("/{id:guid}/projects", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListCIProjectsQuery(id)))).WithName("ListDatabaseProjects");

        // ── Licence ─────────────────────────────────────────────
        var licences = app.MapGroup("/api/pm/licences").WithTags("PM - Licences");
        licences.MapGet("/", async (ISender mediator, string? status, string? licenceType) =>
            Results.Ok(await mediator.Send(new ListLicencesQuery(status, licenceType)))).WithName("ListLicences");
        licences.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetLicenceQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetLicence");
        licences.MapPost("/", async (CreateLicenceRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateLicenceCommand(req.Name, req.Code, req.Description,
                req.LicenceType ?? "Subscription", req.Criticality ?? "Medium", req.OwnerUserId,
                req.Vendor, req.ProductName, req.LicenceKey,
                req.MaxUsers, req.CurrentUsers,
                req.ExpirationDate, req.PurchaseDate, req.AnnualCost, req.Currency));
            return Results.Created($"/api/pm/licences/{id}", new { id });
        }).WithName("CreateLicence");
        licences.MapPut("/{id:guid}", async (Guid id, UpdateLicenceRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateLicenceCommand(id, req.Name, req.Description,
                req.LicenceType, req.Status, req.Criticality, req.OwnerUserId,
                req.Vendor, req.ProductName, req.LicenceKey,
                req.MaxUsers, req.CurrentUsers,
                req.ExpirationDate, req.PurchaseDate, req.AnnualCost, req.Currency));
            return Results.Ok(new { id });
        }).WithName("UpdateLicence");
        licences.MapGet("/{id:guid}/projects", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListCIProjectsQuery(id)))).WithName("ListLicenceProjects");

        // ── CI Relationships ────────────────────────────────────
        var ciRels = app.MapGroup("/api/pm/ci").WithTags("PM - CI Relationships");
        ciRels.MapGet("/{id:guid}/relationships", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListCIRelationshipsQuery(id)))).WithName("ListCIRelationships");
        ciRels.MapPost("/{id:guid}/relationships", async (Guid id, AddCIRelationshipRequest req, ISender mediator) =>
        {
            var relId = await mediator.Send(new AddCIRelationshipCommand(
                id, req.TargetCIId, req.RelationType, req.Notes));
            return Results.Created($"/api/pm/ci/{id}/relationships", new { id = relId });
        }).WithName("AddCIRelationship");
        ciRels.MapDelete("/relationships/{relId:guid}", async (Guid relId, ISender mediator) =>
        {
            await mediator.Send(new RemoveCIRelationshipCommand(relId));
            return Results.NoContent();
        }).WithName("RemoveCIRelationship");

        // ── Milestone ──────────────────────────────────────────────────────
        var milestones = app.MapGroup("/api/pm/projects/{projectId:guid}/milestones").WithTags("PM - Milestones");
        milestones.MapGet("/", async (Guid projectId, ISender mediator, string? status) =>
            Results.Ok(await mediator.Send(new ListMilestonesQuery(projectId, status)))).WithName("ListMilestones");
        milestones.MapPost("/", async (Guid projectId, CreateMilestoneRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateMilestoneCommand(projectId, req.Name, req.DueDate, req.Description, req.SortOrder));
            return Results.Created($"/api/pm/projects/{projectId}/milestones", new { id });
        }).WithName("CreateMilestone");

        var milestoneById = app.MapGroup("/api/pm/milestones").WithTags("PM - Milestones");
        milestoneById.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var m = await mediator.Send(new GetMilestoneQuery(id));
            return m is not null ? Results.Ok(m) : Results.NotFound();
        }).WithName("GetMilestone");
        milestoneById.MapPut("/{id:guid}", async (Guid id, UpdateMilestoneRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new UpdateMilestoneCommand(id, req.Name, req.Description, req.DueDate, req.SortOrder, req.Status));
            return Results.Ok(new { id = result });
        }).WithName("UpdateMilestone");
        milestoneById.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteMilestoneCommand(id));
            return Results.NoContent();
        }).WithName("DeleteMilestone");

        var tasks = app.MapGroup("/api/pm/work-items").WithTags("PM - Work Items");
        tasks.MapGet("/", async (ISender mediator, Guid? projectId, string? status, string? assignee, string? priority,
            int page = 1, int pageSize = 20, Guid? reporterUserId = null, string? assigneeUserIds = null,
            string? type = null, string? sourceFilter = null)
            => Results.Ok(await mediator.Send(new ListWorkItemsQuery(projectId, status, assignee, priority, page, pageSize,
                reporterUserId, assigneeUserIds, type, sourceFilter)))).WithName("ListTasks");
        tasks.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetWorkItemQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetTask");
        tasks.MapPost("/", async (CreateWorkItemRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateWorkItemCommand(req.ProjectId, req.Title, req.Type, req.Priority,
                req.Description, req.AssigneeUserId, req.ReporterUserId, req.ParentTaskId, req.DueDate, req.EstimatedHours, req.Tags));
            return Results.Created($"/api/pm/work-items/{result.Id}", result);
        }).WithName("CreateTask");
        tasks.MapPost("/{id:guid}/move", async (Guid id, MoveTaskRequest req, ISender mediator) =>
            Results.Ok(await mediator.Send(new MoveWorkItemCommand(id, req.Status, req.SortOrder)))).WithName("MoveTask").WithSummary("Kanban sürükle-bırak");
        tasks.MapPost("/{id:guid}/assign", async (Guid id, AssignTaskRequest req, ISender mediator) =>
        {
            var userId = await mediator.Send(new AssignWorkItemCommand(id, req.UserId));
            return Results.Ok(new { id, assigneeUserId = userId });
        }).WithName("AssignTask");
        tasks.MapPut("/{id:guid}", async (Guid id, UpdateWorkItemRequest req, ISender mediator) =>
        {
            var taskId = await mediator.Send(new UpdateWorkItemCommand(id, req.Title, req.Description,
                req.Priority, req.Type, req.DueDate, req.EstimatedHours, req.Tags, req.AssigneeUserId,
                req.StoryPoints, req.AcceptanceCriteria, req.SprintId,
                req.BusinessValue, req.TimeCriticality, req.RiskReduction));
            return Results.Ok(new { id = taskId });
        }).WithName("UpdateTask").WithSummary("Görev bilgilerini günceller");
        tasks.MapPost("/{id:guid}/sprint", async (Guid id, AssignToSprintRequest req, ISender mediator) =>
            Results.Ok(new { id = await mediator.Send(new AssignToSprintCommand(id, req.SprintId)) }))
            .WithName("AssignTaskToSprint").WithSummary("Work item'ı sprint'e ata veya çıkar");
        tasks.MapGet("/board/{projectId:guid}", async (Guid projectId, ISender mediator,
            Guid? sprintId = null, Guid? assigneeUserId = null, bool includeCompleted = false)
            => Results.Ok(await mediator.Send(new GetKanbanBoardQuery(projectId, sprintId, assigneeUserId, includeCompleted))))
            .WithName("KanbanBoard").WithSummary("Kanban board — duruma göre gruplu, filtrelenebilir");

        // ── Cross-Module Source Endpoints ────────────────────────
        tasks.MapGet("/by-source", async (ISender mediator, string module, string type, Guid sourceId)
            => Results.Ok(await mediator.Send(new ListWorkItemsBySourceQuery(module, type, sourceId))))
            .WithName("ListTasksBySource").WithSummary("Kaynağa bağlı görevleri listeler (Ticket vb.)");

        tasks.MapPost("/from-source", async (CreateWorkItemFromSourceRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateWorkItemFromSourceCommand(
                req.SourceModule, req.SourceType, req.SourceId,
                req.Title, req.Description, req.AssigneeUserId, req.ReporterUserId,
                req.Priority, req.DueDate, req.ProjectId,
                req.WorkItemType ?? "Task", req.ParentWorkItemId));
            return Results.Created($"/api/pm/work-items/{result.Id}", result);
        }).WithName("CreateWorkItemFromSource").WithSummary("Dış kaynaktan iş kalemi oluşturur (Ticket → WorkItem)");

        tasks.MapPost("/promote-ticket", async (PromoteTicketRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new PromoteTicketToProjectCommand(
                req.TicketId, req.ProjectId, req.Title,
                req.WorkItemType ?? "Feature", req.Priority ?? "Medium",
                req.Description));
            return Results.Created($"/api/pm/work-items/{result.ParentWorkItemId}",
                new { parentWorkItemId = result.ParentWorkItemId, movedTaskCount = result.MovedTaskCount });
        }).WithName("PromoteTicketToProject").WithSummary("Ticket'ı projeye aktarır, mevcut task'ları da taşır");

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

        // ── Board Columns ────────────────────────────────────────
        var boardCols = app.MapGroup("/api/pm/projects/{projectId:guid}/board-columns").WithTags("PM - Board");
        boardCols.MapGet("/", async (Guid projectId, ISender mediator) =>
        {
            var columns = await mediator.Send(new ListBoardColumnsQuery(projectId));
            return Results.Ok(columns);
        }).WithName("ListBoardColumns").WithSummary("Projenin board kolonlarını listeler");

        boardCols.MapPost("/", async (Guid projectId, CreateBoardColumnRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateBoardColumnCommand(
                projectId, req.Name, req.Order, req.MappedStatus, req.WipLimit));
            return Results.Created($"/api/pm/board-columns/{id}", new { id });
        }).WithName("CreateBoardColumn").WithSummary("Yeni board kolonu ekler");

        boardCols.MapPut("/reorder", async (Guid projectId, ReorderBoardColumnsRequest req, ISender mediator) =>
        {
            await mediator.Send(new ReorderBoardColumnsCommand(projectId, req.ColumnIds));
            return Results.Ok();
        }).WithName("ReorderBoardColumns").WithSummary("Board kolonlarını yeniden sıralar");

        var boardCol = app.MapGroup("/api/pm/board-columns").WithTags("PM - Board");
        boardCol.MapPut("/{id:guid}", async (Guid id, UpdateBoardColumnRequest req, ISender mediator) =>
        {
            var colId = await mediator.Send(new UpdateBoardColumnCommand(
                id, req.Name, req.Order, req.WipLimit, req.MappedStatus));
            return Results.Ok(new { id = colId });
        }).WithName("UpdateBoardColumn").WithSummary("Board kolonu günceller");

        boardCol.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteBoardColumnCommand(id));
            return Results.NoContent();
        }).WithName("DeleteBoardColumn").WithSummary("Board kolonu siler");

        // ── Velocity & Burndown ──────────────────────────────────
        var metrics = app.MapGroup("/api/pm").WithTags("PM - Metrics");
        metrics.MapGet("/projects/{projectId:guid}/velocity", async (Guid projectId, ISender mediator) =>
            Results.Ok(await mediator.Send(new GetVelocityQuery(projectId))))
            .WithName("GetVelocity").WithSummary("Sprint bazlı velocity (bar chart data)");

        metrics.MapGet("/sprints/{sprintId:guid}/burndown", async (Guid sprintId, ISender mediator) =>
            Results.Ok(await mediator.Send(new GetBurndownQuery(sprintId))))
            .WithName("GetBurndown").WithSummary("Sprint burndown (line chart data)");

        metrics.MapGet("/projects/{projectId:guid}/metrics-summary", async (Guid projectId, ISender mediator) =>
            Results.Ok(await mediator.Send(new GetMetricsSummaryQuery(projectId))))
            .WithName("GetMetricsSummary").WithSummary("Proje metrik özeti (dağılım, kanban metrikleri)");

        // ── Project Template ──────────────────────────────────────
        var templates = app.MapGroup("/api/pm/project-templates").WithTags("PM - Project Templates");
        templates.MapGet("/", async (ISender mediator, bool includeInactive = false) =>
            Results.Ok(await mediator.Send(new ListProjectTemplatesQuery(includeInactive))))
            .WithName("ListProjectTemplates");
        templates.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetProjectTemplateQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetProjectTemplate");
        templates.MapPost("/", async (CreateProjectTemplateRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateProjectTemplateCommand(req.Name, req.Description,
                req.Icon, req.Methodology, req.Category, req.EstimationMode, req.SortOrder,
                req.BoardColumnsJson, req.MilestonesJson, req.WorkItemsJson));
            return Results.Created($"/api/pm/project-templates/{id}", new { id });
        }).WithName("CreateProjectTemplate");
        templates.MapPut("/{id:guid}", async (Guid id, UpdateProjectTemplateRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateProjectTemplateCommand(id, req.Name, req.Description,
                req.Icon, req.Methodology, req.Category, req.EstimationMode,
                req.SortOrder, req.IsActive,
                req.BoardColumnsJson, req.MilestonesJson, req.WorkItemsJson));
            return Results.Ok(new { id });
        }).WithName("UpdateProjectTemplate");
        templates.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteProjectTemplateCommand(id));
            return Results.NoContent();
        }).WithName("DeleteProjectTemplate");

        // Template'den proje oluştur
        proj.MapPost("/from-template", async (CreateProjectFromTemplateRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateProjectFromTemplateCommand(
                req.TemplateId, req.Key, req.Name, req.Description,
                req.StartDate, req.TargetEndDate,
                req.ManagerUserId, req.OwnerUserId, req.PortfolioId));
            return Results.Created($"/api/pm/projects/{id}", new { id });
        }).WithName("CreateProjectFromTemplate").WithTags("PM - Projects");

        // ── Requirement ──────────────────────────────────────────
        proj.MapGet("/{id:guid}/requirements", async (Guid id, ISender mediator, string? type, string? status, Guid? parentId) =>
            Results.Ok(await mediator.Send(new ListRequirementsQuery(id, parentId, type, status))))
            .WithName("ListProjectRequirements").WithTags("PM - Requirements");

        proj.MapPost("/{id:guid}/requirements", async (Guid id, CreateRequirementRequest req, ISender mediator) =>
        {
            var rid = await mediator.Send(new CreateRequirementCommand(id, req.Title, req.Type, req.Priority,
                req.Description, req.AcceptanceCriteria, req.ParentRequirementId,
                req.SourceTicketId, req.SourceTicketNumber, req.ExternalDesignUrl));
            return Results.Created($"/api/pm/requirements/{rid}", new { id = rid });
        }).WithName("CreateRequirement").WithTags("PM - Requirements");

        var requirements = app.MapGroup("/api/pm/requirements").WithTags("PM - Requirements");
        requirements.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetRequirementQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetRequirement");

        requirements.MapPut("/{id:guid}", async (Guid id, UpdateRequirementRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateRequirementCommand(id, req.Title, req.Description,
                req.Type, req.Priority, req.Status, req.AcceptanceCriteria, req.ExternalDesignUrl));
            return Results.Ok(new { id });
        }).WithName("UpdateRequirement");

        requirements.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteRequirementCommand(id));
            return Results.NoContent();
        }).WithName("DeleteRequirement");

        // ── Test Scenario ──────────────────────────────────────
        proj.MapGet("/{id:guid}/test-scenarios", async (Guid id, ISender mediator, string? type, string? status, string? priority, Guid? requirementId) =>
            Results.Ok(await mediator.Send(new ListTestScenariosQuery(id, type, status, priority, requirementId))))
            .WithName("ListProjectTestScenarios").WithTags("PM - Test Scenarios");

        proj.MapPost("/{id:guid}/test-scenarios", async (Guid id, CreateTestScenarioRequest req, ISender mediator) =>
        {
            var sid = await mediator.Send(new CreateTestScenarioCommand(id, req.Title, req.Type, req.Priority,
                req.Description, req.Preconditions, req.RequirementId, req.EstimatedDurationMinutes, req.Tags));
            return Results.Created($"/api/pm/test-scenarios/{sid}", new { id = sid });
        }).WithName("CreateTestScenario").WithTags("PM - Test Scenarios");

        var testScenarios = app.MapGroup("/api/pm/test-scenarios").WithTags("PM - Test Scenarios");
        testScenarios.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetTestScenarioQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetTestScenario");

        testScenarios.MapPut("/{id:guid}", async (Guid id, UpdateTestScenarioRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateTestScenarioCommand(id, req.Title, req.Description,
                req.Type, req.Priority, req.Status, req.Preconditions, req.RequirementId,
                req.EstimatedDurationMinutes, req.Tags));
            return Results.Ok(new { id });
        }).WithName("UpdateTestScenario");

        testScenarios.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteTestScenarioCommand(id));
            return Results.NoContent();
        }).WithName("DeleteTestScenario");

        testScenarios.MapPut("/{id:guid}/steps", async (Guid id, UpdateTestStepsRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateTestStepsCommand(id, req.Steps));
            return Results.Ok(new { id });
        }).WithName("UpdateTestSteps");

        testScenarios.MapGet("/{id:guid}/executions", async (Guid id, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListScenarioExecutionsQuery(id))))
            .WithName("ListScenarioExecutions");

        // ── Test Plan ────────────────────────────────────────
        proj.MapGet("/{id:guid}/test-plans", async (Guid id, ISender mediator, string? status) =>
            Results.Ok(await mediator.Send(new ListTestPlansQuery(id, status))))
            .WithName("ListProjectTestPlans").WithTags("PM - Test Plans");

        proj.MapPost("/{id:guid}/test-plans", async (Guid id, CreateTestPlanRequest req, ISender mediator) =>
        {
            var pid = await mediator.Send(new CreateTestPlanCommand(id, req.Title,
                req.Description, req.SprintId, req.MilestoneId,
                req.StartDate, req.EndDate, req.AssignedTesterId));
            return Results.Created($"/api/pm/test-plans/{pid}", new { id = pid });
        }).WithName("CreateTestPlan").WithTags("PM - Test Plans");

        var testPlans = app.MapGroup("/api/pm/test-plans").WithTags("PM - Test Plans");
        testPlans.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetTestPlanQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetTestPlan");

        testPlans.MapPut("/{id:guid}", async (Guid id, UpdateTestPlanRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateTestPlanCommand(id, req.Title, req.Description,
                req.Status, req.StartDate, req.EndDate, req.AssignedTesterId));
            return Results.Ok(new { id });
        }).WithName("UpdateTestPlan");

        testPlans.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteTestPlanCommand(id));
            return Results.NoContent();
        }).WithName("DeleteTestPlan");

        // Plan-Senaryo atama
        testPlans.MapPost("/{planId:guid}/scenarios", async (Guid planId, AddScenarioToTestPlanRequest req, ISender mediator) =>
        {
            var psId = await mediator.Send(new AddScenarioToTestPlanCommand(planId, req.TestScenarioId, req.AssignedTesterId));
            return Results.Created($"/api/pm/test-plans/{planId}", new { id = psId });
        }).WithName("AddScenarioToTestPlan");

        testPlans.MapDelete("/{planId:guid}/scenarios/{scenarioId:guid}", async (Guid planId, Guid scenarioId, ISender mediator) =>
        {
            await mediator.Send(new RemoveScenarioFromTestPlanCommand(planId, scenarioId));
            return Results.NoContent();
        }).WithName("RemoveScenarioFromTestPlan");

        // Test Execution
        testPlans.MapPost("/{planId:guid}/scenarios/{scenarioId:guid}/execute", async (Guid planId, Guid scenarioId, RecordTestExecutionRequest req, ISender mediator) =>
        {
            var eid = await mediator.Send(new RecordTestExecutionCommand(planId, scenarioId,
                req.Result, req.ExecutedBy, req.Notes, req.Environment, req.DurationMinutes, req.StepResults));
            return Results.Created($"/api/pm/test-plans/{planId}", new { id = eid });
        }).WithName("RecordTestExecution");

        testPlans.MapGet("/{planId:guid}/executions", async (Guid planId, ISender mediator, Guid? scenarioId) =>
            Results.Ok(await mediator.Send(new ListTestExecutionsQuery(planId, scenarioId))))
            .WithName("ListTestPlanExecutions");

        // ── Release ──────────────────────────────────────────
        proj.MapGet("/{id:guid}/releases", async (Guid id, ISender mediator, string? status, string? type) =>
            Results.Ok(await mediator.Send(new ListReleasesQuery(id, status, type))))
            .WithName("ListProjectReleases").WithTags("PM - Releases");

        proj.MapPost("/{id:guid}/releases", async (Guid id, CreateReleaseRequest req, ISender mediator) =>
        {
            var rid = await mediator.Send(new CreateReleaseCommand(id, req.Version, req.Title,
                req.Type, req.Description, req.SprintId, req.MilestoneId,
                req.PlannedDate, req.CodeFreezeDate,
                req.ReleaseManagerId, req.TargetEnvironment, req.Tags));
            return Results.Created($"/api/pm/releases/{rid}", new { id = rid });
        }).WithName("CreateRelease").WithTags("PM - Releases");

        var releases = app.MapGroup("/api/pm/releases").WithTags("PM - Releases");
        releases.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetReleaseQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetRelease");

        releases.MapPut("/{id:guid}", async (Guid id, UpdateReleaseRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateReleaseCommand(id, req.Version, req.Title, req.Description,
                req.Type, req.PlannedDate, req.ActualDate, req.CodeFreezeDate,
                req.ReleaseManagerId, req.TargetEnvironment, req.Tags));
            return Results.Ok(new { id });
        }).WithName("UpdateRelease");

        releases.MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteReleaseCommand(id));
            return Results.NoContent();
        }).WithName("DeleteRelease");

        releases.MapPut("/{id:guid}/status", async (Guid id, UpdateReleaseStatusRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateReleaseStatusCommand(id, req.Status));
            return Results.Ok(new { id });
        }).WithName("UpdateReleaseStatus");

        // Release Items
        releases.MapGet("/{releaseId:guid}/items", async (Guid releaseId, ISender mediator) =>
            Results.Ok(await mediator.Send(new ListReleaseItemsQuery(releaseId))))
            .WithName("ListReleaseItems");

        releases.MapPost("/{releaseId:guid}/items", async (Guid releaseId, AddReleaseItemRequest req, ISender mediator) =>
        {
            var itemId = await mediator.Send(new AddReleaseItemCommand(releaseId, req.WorkItemId, req.Notes));
            return Results.Created($"/api/pm/releases/{releaseId}/items", new { id = itemId });
        }).WithName("AddReleaseItem");

        releases.MapDelete("/{releaseId:guid}/items/{workItemId:guid}", async (Guid releaseId, Guid workItemId, ISender mediator) =>
        {
            await mediator.Send(new RemoveReleaseItemCommand(releaseId, workItemId));
            return Results.NoContent();
        }).WithName("RemoveReleaseItem");

        releases.MapPost("/{releaseId:guid}/items/from-sprint", async (Guid releaseId, AddItemsFromSprintRequest req, ISender mediator) =>
        {
            var count = await mediator.Send(new AddReleaseItemsFromSprintCommand(releaseId, req.SprintId));
            return Results.Ok(new { addedCount = count });
        }).WithName("AddReleaseItemsFromSprint").WithSummary("Sprint'teki Done WorkItem'ları release'e ekler");

        // Go/No-Go
        releases.MapGet("/{releaseId:guid}/go-no-go", async (Guid releaseId, ISender mediator) =>
        { var r = await mediator.Send(new GetGoNoGoChecklistQuery(releaseId)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetGoNoGoChecklist");

        releases.MapPost("/{releaseId:guid}/go-no-go", async (Guid releaseId, ISender mediator) =>
        {
            var cid = await mediator.Send(new CreateGoNoGoChecklistCommand(releaseId));
            return Results.Created($"/api/pm/releases/{releaseId}/go-no-go", new { id = cid });
        }).WithName("CreateGoNoGoChecklist").WithSummary("Varsayılan maddelerle checklist oluşturur");

        releases.MapPost("/{releaseId:guid}/go-no-go/items", async (Guid releaseId, AddGoNoGoItemRequest req, ISender mediator) =>
        {
            var itemId = await mediator.Send(new AddGoNoGoItemCommand(releaseId,
                req.Category, req.Title, req.Description, req.IsRequired));
            return Results.Created($"/api/pm/releases/{releaseId}/go-no-go", new { id = itemId });
        }).WithName("AddGoNoGoItem");

        releases.MapPut("/{releaseId:guid}/go-no-go/items/{itemId:guid}", async (Guid releaseId, Guid itemId, UpdateGoNoGoItemRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateGoNoGoItemCommand(releaseId, itemId, req.Status, req.ReviewedBy, req.Notes));
            return Results.Ok(new { id = itemId });
        }).WithName("UpdateGoNoGoItem");

        releases.MapPut("/{releaseId:guid}/go-no-go/decide", async (Guid releaseId, DecideGoNoGoRequest req, ISender mediator) =>
        {
            await mediator.Send(new DecideGoNoGoCommand(releaseId, req.Status, req.DecisionBy, req.DecisionNotes));
            return Results.Ok(new { releaseId });
        }).WithName("DecideGoNoGo").WithSummary("Genel Go/No-Go kararı (Approved/Rejected)");

        // Release Note
        releases.MapGet("/{releaseId:guid}/release-note", async (Guid releaseId, ISender mediator) =>
        { var r = await mediator.Send(new GetReleaseNoteQuery(releaseId)); return r is null ? Results.NotFound() : Results.Ok(r); })
            .WithName("GetReleaseNote");

        releases.MapPost("/{releaseId:guid}/release-note/generate", async (Guid releaseId, ISender mediator) =>
        {
            var nid = await mediator.Send(new GenerateReleaseNoteCommand(releaseId));
            return Results.Ok(new { id = nid });
        }).WithName("GenerateReleaseNote").WithSummary("WorkItem'lardan otomatik release note üretir");

        releases.MapPut("/{releaseId:guid}/release-note", async (Guid releaseId, UpdateReleaseNoteRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateReleaseNoteCommand(releaseId, req.Content));
            return Results.Ok(new { releaseId });
        }).WithName("UpdateReleaseNote");

        releases.MapGet("/{releaseId:guid}/release-note/export", async (Guid releaseId, ISender mediator, string format = "markdown") =>
        {
            var note = await mediator.Send(new GetReleaseNoteQuery(releaseId));
            if (note is null) return Results.NotFound();
            return Results.Text(note.Content, "text/markdown");
        }).WithName("ExportReleaseNote").WithSummary("Release note'u Markdown olarak export eder");

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
public sealed record CreateWorkItemRequest(Guid? ProjectId, string Title, string Type = "Task",
    string Priority = "Medium", string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, Guid? ParentTaskId = null, DateTime? DueDate = null,
    decimal EstimatedHours = 0, string? Tags = null);
public sealed record CreateWorkItemFromSourceRequest(
    string SourceModule, string SourceType, Guid SourceId,
    string Title, string? Description = null, Guid? AssigneeUserId = null,
    Guid? ReporterUserId = null, string Priority = "Medium",
    DateTime? DueDate = null, Guid? ProjectId = null,
    string? WorkItemType = "Task", Guid? ParentWorkItemId = null);
public sealed record MoveTaskRequest(string Status, int? SortOrder = null);
public sealed record AssignTaskRequest(Guid? UserId);
public sealed record CreateCommentRequest(Guid TaskId, Guid AuthorUserId, string Content);
public sealed record CreateTimeEntryRequest(Guid TaskId, Guid UserId, decimal Hours,
    DateTime WorkDate, string? Description = null);
public sealed record UpdateWorkItemRequest(string? Title = null, string? Description = null,
    string? Priority = null, string? Type = null, DateTime? DueDate = null,
    decimal? EstimatedHours = null, string? Tags = null, Guid? AssigneeUserId = null,
    int? StoryPoints = null, string? AcceptanceCriteria = null, Guid? SprintId = null,
    int? BusinessValue = null, int? TimeCriticality = null, int? RiskReduction = null);
public sealed record AddDeliverableRequest(Guid ConfigurationItemId, string? Role = "Primary", string? Notes = null);
public sealed record AssignToSprintRequest(Guid? SprintId);

// Sprint
public sealed record CreateSprintRequest(string Name, DateTime StartDate, DateTime EndDate,
    string? Goal = null, int? CapacityPoints = null);
public sealed record UpdateSprintRequest(string? Name = null, string? Goal = null,
    DateTime? StartDate = null, DateTime? EndDate = null, int? CapacityPoints = null);

// BoardColumn
public sealed record CreateBoardColumnRequest(string Name, int Order,
    string MappedStatus, int? WipLimit = null);
public sealed record UpdateBoardColumnRequest(string? Name = null,
    int? Order = null, int? WipLimit = null, string? MappedStatus = null);
public sealed record ReorderBoardColumnsRequest(List<Guid> ColumnIds);

// Server
public sealed record CreateServerRequest(string Name, string Code,
    string? Description = null, string? ServerType = null, string? Environment = null,
    string? Criticality = null, Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? OperatingSystem = null, string? IpAddress = null, string? Hostname = null,
    int? CpuCores = null, int? RamGB = null, int? DiskGB = null, string? DataCenter = null);
public sealed record UpdateServerRequest(string? Name = null, string? Description = null,
    string? ServerType = null, string? Environment = null, string? Status = null,
    string? Criticality = null, Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? OperatingSystem = null, string? IpAddress = null, string? Hostname = null,
    int? CpuCores = null, int? RamGB = null, int? DiskGB = null, string? DataCenter = null);

// Database
public sealed record CreateDatabaseRequest(string Name, string Code,
    string? Description = null, string? DatabaseEngine = null, string? Criticality = null,
    Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? Version = null, int? Port = null, decimal? SizeGB = null,
    string? ConnectionString = null, string? BackupSchedule = null);
public sealed record UpdateDatabaseRequest(string? Name = null, string? Description = null,
    string? DatabaseEngine = null, string? Status = null, string? Criticality = null,
    Guid? OwnerUserId = null, Guid? AdminUserId = null,
    string? Version = null, int? Port = null, decimal? SizeGB = null,
    string? ConnectionString = null, string? BackupSchedule = null);

// Licence
public sealed record CreateLicenceRequest(string Name, string Code,
    string? Description = null, string? LicenceType = null, string? Criticality = null,
    Guid? OwnerUserId = null,
    string? Vendor = null, string? ProductName = null, string? LicenceKey = null,
    int? MaxUsers = null, int? CurrentUsers = null,
    DateTime? ExpirationDate = null, DateTime? PurchaseDate = null,
    decimal? AnnualCost = null, string? Currency = null);
public sealed record UpdateLicenceRequest(string? Name = null, string? Description = null,
    string? LicenceType = null, string? Status = null, string? Criticality = null,
    Guid? OwnerUserId = null,
    string? Vendor = null, string? ProductName = null, string? LicenceKey = null,
    int? MaxUsers = null, int? CurrentUsers = null,
    DateTime? ExpirationDate = null, DateTime? PurchaseDate = null,
    decimal? AnnualCost = null, string? Currency = null);

// CI Relationship
public sealed record AddCIRelationshipRequest(Guid TargetCIId, string RelationType, string? Notes = null);

// Milestone
public sealed record CreateMilestoneRequest(string Name, DateTime DueDate, string? Description = null, int SortOrder = 0);
public sealed record UpdateMilestoneRequest(string? Name = null, string? Description = null, DateTime? DueDate = null, int? SortOrder = null, string? Status = null);

// Ticket Promotion
public sealed record PromoteTicketRequest(Guid TicketId, Guid ProjectId, string Title,
    string? WorkItemType = "Feature", string? Priority = "Medium", string? Description = null);

// Project Template
public sealed record CreateProjectTemplateRequest(string Name, string? Description = null,
    string? Icon = null, string? Methodology = null, string? Category = null,
    string? EstimationMode = null, int SortOrder = 0,
    string? BoardColumnsJson = null, string? MilestonesJson = null,
    string? WorkItemsJson = null);
public sealed record UpdateProjectTemplateRequest(string? Name = null, string? Description = null,
    string? Icon = null, string? Methodology = null, string? Category = null,
    string? EstimationMode = null, int? SortOrder = null, bool? IsActive = null,
    string? BoardColumnsJson = null, string? MilestonesJson = null,
    string? WorkItemsJson = null);
public sealed record CreateProjectFromTemplateRequest(Guid TemplateId,
    string Key, string Name, string? Description = null,
    DateTime? StartDate = null, DateTime? TargetEndDate = null,
    Guid? ManagerUserId = null, Guid? OwnerUserId = null, Guid? PortfolioId = null);

// Requirement
public sealed record CreateRequirementRequest(string Title,
    string Type = "Functional", string Priority = "Must",
    string? Description = null, string? AcceptanceCriteria = null,
    Guid? ParentRequirementId = null,
    Guid? SourceTicketId = null, string? SourceTicketNumber = null,
    string? ExternalDesignUrl = null);
public sealed record UpdateRequirementRequest(string? Title = null, string? Description = null,
    string? Type = null, string? Priority = null, string? Status = null,
    string? AcceptanceCriteria = null, string? ExternalDesignUrl = null);

// Test Scenario
public sealed record CreateTestScenarioRequest(string Title,
    string Type = "Functional", string Priority = "Medium",
    string? Description = null, string? Preconditions = null,
    Guid? RequirementId = null, int? EstimatedDurationMinutes = null, string? Tags = null);
public sealed record UpdateTestScenarioRequest(string? Title = null, string? Description = null,
    string? Type = null, string? Priority = null, string? Status = null,
    string? Preconditions = null, Guid? RequirementId = null,
    int? EstimatedDurationMinutes = null, string? Tags = null);
public sealed record UpdateTestStepsRequest(List<TestStepDto> Steps);

// Test Plan
public sealed record CreateTestPlanRequest(string Title,
    string? Description = null, Guid? SprintId = null, Guid? MilestoneId = null,
    string? StartDate = null, string? EndDate = null, string? AssignedTesterId = null);
public sealed record UpdateTestPlanRequest(string? Title = null, string? Description = null,
    string? Status = null, string? StartDate = null, string? EndDate = null,
    string? AssignedTesterId = null);

// Test Plan Scenario
public sealed record AddScenarioToTestPlanRequest(Guid TestScenarioId, string? AssignedTesterId = null);

// Test Execution
public sealed record RecordTestExecutionRequest(string Result,
    string? ExecutedBy = null, string? Notes = null, string? Environment = null,
    int? DurationMinutes = null, List<TestStepResultDto>? StepResults = null);

// ── Release Request DTOs ────────────────────────────────────
public sealed record CreateReleaseRequest(string Version, string Title,
    string Type = "Minor", string? Description = null,
    Guid? SprintId = null, Guid? MilestoneId = null,
    string? PlannedDate = null, string? CodeFreezeDate = null,
    string? ReleaseManagerId = null, string? TargetEnvironment = null,
    string? Tags = null);

public sealed record UpdateReleaseRequest(
    string? Version = null, string? Title = null, string? Description = null,
    string? Type = null,
    string? PlannedDate = null, string? ActualDate = null, string? CodeFreezeDate = null,
    string? ReleaseManagerId = null, string? TargetEnvironment = null,
    string? Tags = null);

public sealed record UpdateReleaseStatusRequest(string Status);
public sealed record AddReleaseItemRequest(Guid WorkItemId, string? Notes = null);
public sealed record AddItemsFromSprintRequest(Guid SprintId);

public sealed record AddGoNoGoItemRequest(string Category, string Title,
    string? Description = null, bool IsRequired = true);

public sealed record UpdateGoNoGoItemRequest(string Status,
    string? ReviewedBy = null, string? Notes = null);

public sealed record DecideGoNoGoRequest(string Status,
    string? DecisionBy = null, string? DecisionNotes = null);

public sealed record UpdateReleaseNoteRequest(string Content);

