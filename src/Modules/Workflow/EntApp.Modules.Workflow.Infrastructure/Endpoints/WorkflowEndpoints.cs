using EntApp.Modules.Workflow.Application.Commands;
using EntApp.Modules.Workflow.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.Workflow.Infrastructure.Endpoints;

/// <summary>Workflow & Approval Engine REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════
        // Definition Endpoints
        // ═══════════════════════════════════════════════════
        var defs = app.MapGroup("/api/workflows/definitions").WithTags("Workflow Definitions");

        defs.MapGet("/", async (ISender mediator, string? category, int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListDefinitionsQuery(category, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListDefinitions");

        defs.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetDefinitionQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetDefinition");

        defs.MapPost("/", async (CreateDefinitionRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateDefinitionCommand(
                req.Name, req.Title, req.Description, req.Category,
                req.ApprovalType, req.StepDefinitionsJson, req.TimeoutHours));
            return Results.Created($"/api/workflows/definitions/{result.Id}", result);
        }).WithName("CreateDefinition");

        // ═══════════════════════════════════════════════════
        // Instance (Workflow) Endpoints
        // ═══════════════════════════════════════════════════
        var wf = app.MapGroup("/api/workflows").WithTags("Workflows");

        wf.MapGet("/", async (ISender mediator, string? status, int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListWorkflowsQuery(status, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListWorkflows");

        wf.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetWorkflowQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetWorkflow");

        wf.MapPost("/start", async (StartWorkflowRequest req, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new StartWorkflowCommand(
                    req.DefinitionId, req.InitiatorUserId, req.ReferenceType, req.ReferenceId, req.Metadata));
                return Results.Created($"/api/workflows/{result.Id}", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithName("StartWorkflow");

        wf.MapPost("/{id:guid}/approve", async (Guid id, ApprovalActionRequest req, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new ApproveStepCommand(id, req.StepId, req.UserId, req.Comment));
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithName("ApproveStep");

        wf.MapPost("/{id:guid}/reject", async (Guid id, ApprovalActionRequest req, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new RejectStepCommand(id, req.StepId, req.UserId, req.Comment));
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithName("RejectStep");

        wf.MapPost("/{id:guid}/escalate", async (Guid id, EscalateRequest req, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new EscalateStepCommand(id, req.StepId, req.EscalateToUserId, req.Comment));
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithName("EscalateStep");

        wf.MapPost("/{id:guid}/cancel", async (Guid id, ISender mediator) =>
        {
            try
            {
                var result = await mediator.Send(new CancelWorkflowCommand(id));
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).WithName("CancelWorkflow");

        wf.MapGet("/pending/{userId:guid}", async (Guid userId, ISender mediator) =>
        {
            var result = await mediator.Send(new GetPendingStepsQuery(userId));
            return Results.Ok(result);
        }).WithName("GetPendingSteps").WithSummary("Kullanıcının onay bekleyen adımlarını listele");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
public sealed record CreateDefinitionRequest(string Name, string Title,
    string? Description = null, string? Category = null,
    string ApprovalType = "Sequential", string? StepDefinitionsJson = null,
    int? TimeoutHours = null);

public sealed record StartWorkflowRequest(Guid DefinitionId,
    Guid? InitiatorUserId = null, string? ReferenceType = null,
    string? ReferenceId = null, string? Metadata = null);

public sealed record ApprovalActionRequest(Guid StepId, Guid UserId, string? Comment = null);

public sealed record EscalateRequest(Guid StepId, Guid EscalateToUserId, string? Comment = null);
