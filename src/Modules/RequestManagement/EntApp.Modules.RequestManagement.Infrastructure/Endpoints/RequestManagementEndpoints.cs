using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Application.Queries;
using EntApp.Modules.RequestManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.RequestManagement.Infrastructure.Endpoints;

/// <summary>Request Management REST API endpoint'leri — ISender thin proxy.</summary>
public static class RequestManagementEndpoints
{
    public static IEndpointRouteBuilder MapRequestManagementEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Departments ═══════════
        var depts = app.MapGroup("/api/req/departments").WithTags("Request Mgmt - Departments");

        depts.MapGet("/", async (ISender mediator, bool? activeOnly) =>
            Results.Ok(await mediator.Send(new ListDepartmentsQuery(activeOnly ?? true))))
            .WithName("ListDepartments");

        depts.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetDepartmentQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetDepartment");

        depts.MapPost("/", async (CreateDepartmentRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateDepartmentCommand(
                req.Name, req.Code, req.Description, req.ManagerUserId, req.ParentDepartmentId));
            return Results.Created($"/api/req/departments/{id}", new { id });
        }).WithName("CreateDepartment");

        depts.MapPut("/{id:guid}", async (Guid id, UpdateDepartmentRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateDepartmentCommand(
                id, req.Name, req.Code, req.Description, req.ManagerUserId, req.ParentDepartmentId));
            return Results.NoContent();
        }).WithName("UpdateDepartment");

        // ═══════════ Categories ═══════════
        var cats = app.MapGroup("/api/req/categories").WithTags("Request Mgmt - Categories");

        cats.MapGet("/", async (ISender mediator, Guid? departmentId, bool? activeOnly) =>
            Results.Ok(await mediator.Send(new ListCategoriesQuery(departmentId, activeOnly ?? true))))
            .WithName("ListCategories");

        cats.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetCategoryQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetCategory");

        cats.MapPost("/", async (CreateCategoryRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateCategoryCommand(
                req.Name, req.Code, req.DepartmentId, req.Description,
                req.SlaDefinitionId, req.WorkflowDefinitionId,
                req.FormSchemaJson, req.AutoProjectThreshold));
            return Results.Created($"/api/req/categories/{id}", new { id });
        }).WithName("CreateCategory");

        cats.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateCategoryCommand(
                id, req.Name, req.Code, req.DepartmentId, req.Description,
                req.SlaDefinitionId, req.WorkflowDefinitionId,
                req.FormSchemaJson, req.AutoProjectThreshold));
            return Results.NoContent();
        }).WithName("UpdateCategory");

        // ═══════════ SLA Definitions ═══════════
        var sla = app.MapGroup("/api/req/sla-definitions").WithTags("Request Mgmt - SLA");

        sla.MapGet("/", async (ISender mediator, bool? activeOnly) =>
            Results.Ok(await mediator.Send(new ListSlaDefinitionsQuery(activeOnly ?? true))))
            .WithName("ListSlaDefinitions");

        sla.MapPost("/", async (CreateSlaRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateSlaCommand(
                req.Name, req.Description, req.ResponseTimeJson, req.ResolutionTimeJson));
            return Results.Created($"/api/req/sla-definitions/{id}", new { id });
        }).WithName("CreateSlaDefinition");

        sla.MapPut("/{id:guid}", async (Guid id, UpdateSlaRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateSlaCommand(
                id, req.Name, req.Description, req.ResponseTimeJson, req.ResolutionTimeJson));
            return Results.NoContent();
        }).WithName("UpdateSlaDefinition");

        // ═══════════ Tickets ═══════════
        var tickets = app.MapGroup("/api/req/tickets").WithTags("Request Mgmt - Tickets");

        tickets.MapGet("/", async (ISender mediator, TicketStatus? status, TicketPriority? priority,
            Guid? assigneeUserId, Guid? departmentId, int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new ListTicketsQuery(
                status, priority, assigneeUserId, departmentId, page, pageSize))))
            .WithName("ListTickets");

        tickets.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetTicketQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetTicket");

        tickets.MapGet("/my/{reporterUserId:guid}", async (Guid reporterUserId, ISender mediator,
            int page = 1, int pageSize = 20) =>
            Results.Ok(await mediator.Send(new GetMyTicketsQuery(reporterUserId, page, pageSize))))
            .WithName("GetMyTickets");

        tickets.MapPost("/", async (CreateTicketRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateTicketCommand(
                req.Title, req.CategoryId, req.DepartmentId,
                req.Description, req.Priority, req.Channel, req.FormDataJson));
            return Results.Created($"/api/req/tickets/{id}", new { id });
        }).WithName("CreateTicket");

        tickets.MapPut("/{id:guid}", async (Guid id, UpdateTicketRequest req, ISender mediator) =>
        {
            await mediator.Send(new UpdateTicketCommand(id, req.Title, req.Description, req.Priority));
            return Results.NoContent();
        }).WithName("UpdateTicket");

        tickets.MapPost("/{id:guid}/assign", async (Guid id, AssignTicketRequest req, ISender mediator) =>
        {
            await mediator.Send(new AssignTicketCommand(id, req.AssigneeUserId));
            return Results.NoContent();
        }).WithName("AssignTicket");

        tickets.MapPost("/{id:guid}/status", async (Guid id, ChangeStatusRequest req, ISender mediator) =>
        {
            await mediator.Send(new ChangeTicketStatusCommand(id, req.NewStatus, req.Reason));
            return Results.NoContent();
        }).WithName("ChangeTicketStatus");

        tickets.MapPost("/{id:guid}/close", async (Guid id, CloseTicketRequest req, ISender mediator) =>
        {
            await mediator.Send(new CloseTicketCommand(id, req.Reason));
            return Results.NoContent();
        }).WithName("CloseTicket");

        // ═══════════ Comments ═══════════
        tickets.MapPost("/{id:guid}/comments", async (Guid id, AddCommentRequest req, ISender mediator) =>
        {
            var commentId = await mediator.Send(new AddCommentCommand(id, req.Content, req.IsInternal));
            return Results.Created($"/api/req/tickets/{id}/comments/{commentId}", new { id = commentId });
        }).WithName("AddTicketComment");

        // ═══════════ Form Schema ═══════════
        cats.MapGet("/{id:guid}/form-schema", async (Guid id, ISender mediator) =>
        {
            var cat = await mediator.Send(new GetCategoryQuery(id));
            if (cat is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(cat.FormSchemaJson)) return Results.Ok(Array.Empty<object>());
            return Results.Text(cat.FormSchemaJson, "application/json");
        }).WithName("GetCategoryFormSchema");

        tickets.MapGet("/{id:guid}/form-data", async (Guid id, ISender mediator) =>
        {
            var ticket = await mediator.Send(new GetTicketQuery(id));
            if (ticket is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(ticket.FormDataJson)) return Results.Ok(new { });
            return Results.Text(ticket.FormDataJson, "application/json");
        }).WithName("GetTicketFormData");

        return app;
    }
}

// ── Request DTOs ──────────────────────────────────────────────
public sealed record CreateDepartmentRequest(string Name, string Code, string? Description, Guid? ManagerUserId, Guid? ParentDepartmentId);
public sealed record UpdateDepartmentRequest(string Name, string Code, string? Description, Guid? ManagerUserId, Guid? ParentDepartmentId);
public sealed record CreateCategoryRequest(string Name, string Code, Guid DepartmentId, string? Description, Guid? SlaDefinitionId, Guid? WorkflowDefinitionId, string? FormSchemaJson, int? AutoProjectThreshold);
public sealed record UpdateCategoryRequest(string Name, string Code, Guid DepartmentId, string? Description, Guid? SlaDefinitionId, Guid? WorkflowDefinitionId, string? FormSchemaJson, int? AutoProjectThreshold);
public sealed record CreateSlaRequest(string Name, string? Description, string? ResponseTimeJson, string? ResolutionTimeJson);
public sealed record UpdateSlaRequest(string Name, string? Description, string? ResponseTimeJson, string? ResolutionTimeJson);
public sealed record CreateTicketRequest(string Title, Guid CategoryId, Guid DepartmentId, string? Description, TicketPriority Priority, TicketChannel Channel, string? FormDataJson = null);
public sealed record UpdateTicketRequest(string Title, string? Description, TicketPriority Priority);
public sealed record AssignTicketRequest(Guid AssigneeUserId);
public sealed record ChangeStatusRequest(TicketStatus NewStatus, string? Reason);
public sealed record CloseTicketRequest(string? Reason);
public sealed record AddCommentRequest(string Content, bool IsInternal);
