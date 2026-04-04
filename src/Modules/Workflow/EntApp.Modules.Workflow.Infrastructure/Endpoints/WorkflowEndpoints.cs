using EntApp.Modules.Workflow.Application.Interfaces;
using EntApp.Modules.Workflow.Domain.Entities;
using EntApp.Modules.Workflow.Domain.Enums;
using EntApp.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Workflow.Infrastructure.Endpoints;

/// <summary>
/// Workflow & Approval Engine REST API endpoint'leri.
/// </summary>
public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════════════════════════════════════════════
        // Definition Endpoints
        // ═══════════════════════════════════════════════════
        var defs = app.MapGroup("/api/workflows/definitions").WithTags("Workflow Definitions");

        defs.MapGet("/", async (WorkflowDbContext db, string? category, int page = 1, int pageSize = 20) =>
        {
            var query = db.Definitions.Where(d => d.IsActive);
            if (!string.IsNullOrEmpty(category))
                query = query.Where(d => d.Category == category);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(d => d.Category).ThenBy(d => d.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(d => new
                {
                    d.Id, d.Name, d.Title, d.Description, d.Category,
                    ApprovalType = d.ApprovalType.ToString(),
                    d.TimeoutHours, d.IsActive, d.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        })
        .WithName("ListDefinitions");

        defs.MapGet("/{id:guid}", async (Guid id, WorkflowDbContext db) =>
        {
            var d = await db.Definitions.FindAsync(id);
            return d is null ? Results.NotFound() : Results.Ok(d);
        })
        .WithName("GetDefinition");

        defs.MapPost("/", async (CreateDefinitionRequest req, WorkflowDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Title))
                return Results.BadRequest(new { error = "Name and Title are required." });

            if (!Enum.TryParse<ApprovalType>(req.ApprovalType, out var approvalType))
                approvalType = ApprovalType.Sequential;

            var def = WorkflowDefinition.Create(
                name: req.Name,
                title: req.Title,
                approvalType: approvalType,
                stepDefinitionsJson: req.StepDefinitionsJson ?? "[]",
                description: req.Description,
                category: req.Category,
                timeoutHours: req.TimeoutHours);

            db.Definitions.Add(def);
            await db.SaveChangesAsync();

            return Results.Created($"/api/workflows/definitions/{def.Id}", new { def.Id, def.Name });
        })
        .WithName("CreateDefinition");

        // ═══════════════════════════════════════════════════
        // Instance (Workflow) Endpoints
        // ═══════════════════════════════════════════════════
        var wf = app.MapGroup("/api/workflows").WithTags("Workflows");

        wf.MapGet("/", async (WorkflowDbContext db, string? status, int page = 1, int pageSize = 20) =>
        {
            var query = db.Instances.Include(i => i.Definition).AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<WorkflowStatus>(status, out var s))
                query = query.Where(i => i.Status == s);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(i => new
                {
                    i.Id, DefinitionName = i.Definition.Name,
                    Status = i.Status.ToString(),
                    i.ReferenceType, i.ReferenceId,
                    i.CurrentStepOrder, i.StartedAt, i.CompletedAt, i.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        })
        .WithName("ListWorkflows");

        wf.MapGet("/{id:guid}", async (Guid id, WorkflowDbContext db) =>
        {
            var instance = await db.Instances
                .Include(i => i.Definition)
                .Include(i => i.Steps.OrderBy(s => s.StepOrder))
                .FirstOrDefaultAsync(i => i.Id == id);

            if (instance is null) return Results.NotFound();

            return Results.Ok(new
            {
                instance.Id,
                Definition = new { instance.Definition.Name, instance.Definition.Title, ApprovalType = instance.Definition.ApprovalType.ToString() },
                Status = instance.Status.ToString(),
                instance.ReferenceType,
                instance.ReferenceId,
                instance.CurrentStepOrder,
                instance.StartedAt,
                instance.CompletedAt,
                Steps = instance.Steps.Select(s => new
                {
                    s.Id, s.StepOrder, s.StepName,
                    Status = s.Status.ToString(),
                    s.AssignedUserId, s.AssignedRole,
                    s.ActionByUserId, s.ActionAt, s.Comment, s.DueDate
                })
            });
        })
        .WithName("GetWorkflow");

        // ── Start ────────────────────────────────────────
        wf.MapPost("/start", async (StartWorkflowRequest req, IWorkflowEngine engine) =>
        {
            try
            {
                var instance = await engine.StartAsync(
                    req.DefinitionId, req.InitiatorUserId,
                    req.ReferenceType, req.ReferenceId, req.Metadata);

                return Results.Created($"/api/workflows/{instance.Id}",
                    new { instance.Id, Status = instance.Status.ToString() });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .WithName("StartWorkflow");

        // ── Approve ──────────────────────────────────────
        wf.MapPost("/{id:guid}/approve", async (Guid id, ApprovalActionRequest req, IWorkflowEngine engine) =>
        {
            try
            {
                var instance = await engine.ApproveAsync(id, req.StepId, req.UserId, req.Comment);
                return Results.Ok(new { instance.Id, Status = instance.Status.ToString() });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .WithName("ApproveStep");

        // ── Reject ───────────────────────────────────────
        wf.MapPost("/{id:guid}/reject", async (Guid id, ApprovalActionRequest req, IWorkflowEngine engine) =>
        {
            try
            {
                var instance = await engine.RejectAsync(id, req.StepId, req.UserId, req.Comment);
                return Results.Ok(new { instance.Id, Status = instance.Status.ToString() });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .WithName("RejectStep");

        // ── Escalate ─────────────────────────────────────
        wf.MapPost("/{id:guid}/escalate", async (Guid id, EscalateRequest req, IWorkflowEngine engine) =>
        {
            try
            {
                var instance = await engine.EscalateAsync(id, req.StepId, req.EscalateToUserId, req.Comment);
                return Results.Ok(new { instance.Id, Status = instance.Status.ToString() });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .WithName("EscalateStep");

        // ── Cancel ───────────────────────────────────────
        wf.MapPost("/{id:guid}/cancel", async (Guid id, IWorkflowEngine engine) =>
        {
            try
            {
                var instance = await engine.CancelAsync(id);
                return Results.Ok(new { instance.Id, Status = instance.Status.ToString() });
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .WithName("CancelWorkflow");

        // ── Pending ──────────────────────────────────────
        wf.MapGet("/pending/{userId:guid}", async (Guid userId, IWorkflowEngine engine) =>
        {
            var steps = await engine.GetPendingStepsAsync(userId);
            return Results.Ok(steps.Select(s => new
            {
                s.Id, s.InstanceId, s.StepOrder, s.StepName,
                s.AssignedUserId, s.DueDate, s.CreatedAt
            }));
        })
        .WithName("GetPendingSteps")
        .WithSummary("Kullanıcının onay bekleyen adımlarını listele");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────

public sealed record CreateDefinitionRequest(
    string Name,
    string Title,
    string? Description = null,
    string? Category = null,
    string ApprovalType = "Sequential",
    string? StepDefinitionsJson = null,
    int? TimeoutHours = null);

public sealed record StartWorkflowRequest(
    Guid DefinitionId,
    Guid? InitiatorUserId = null,
    string? ReferenceType = null,
    string? ReferenceId = null,
    string? Metadata = null);

public sealed record ApprovalActionRequest(
    Guid StepId,
    Guid UserId,
    string? Comment = null);

public sealed record EscalateRequest(
    Guid StepId,
    Guid EscalateToUserId,
    string? Comment = null);
