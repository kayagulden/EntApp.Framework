using EntApp.Modules.Workflow.Application.Commands;
using EntApp.Modules.Workflow.Application.Interfaces;
using EntApp.Modules.Workflow.Application.Queries;
using EntApp.Modules.Workflow.Domain.Entities;
using EntApp.Modules.Workflow.Domain.Enums;
using EntApp.Modules.Workflow.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Workflow.Infrastructure.Handlers;

// ── Definition Handlers ──────────────────────────────────────
public sealed class ListDefinitionsQueryHandler(WorkflowDbContext db)
    : IRequestHandler<ListDefinitionsQuery, PagedResult<DefinitionListItem>>
{
    public async Task<PagedResult<DefinitionListItem>> Handle(ListDefinitionsQuery request, CancellationToken ct)
    {
        var query = db.Definitions.Where(d => d.IsActive);
        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(d => d.Category == request.Category);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.Category).ThenBy(d => d.Name)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(d => new DefinitionListItem(d.Id, d.Name, d.Title, d.Description,
                d.Category, d.ApprovalType.ToString(), d.TimeoutHours, d.IsActive, d.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<DefinitionListItem>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }
}

public sealed class GetDefinitionQueryHandler(WorkflowDbContext db)
    : IRequestHandler<GetDefinitionQuery, object?>
{
    public async Task<object?> Handle(GetDefinitionQuery request, CancellationToken ct)
    {
        var d = await db.Definitions.FindAsync([request.Id], ct);
        return d;
    }
}

public sealed class CreateDefinitionCommandHandler(WorkflowDbContext db)
    : IRequestHandler<CreateDefinitionCommand, CreateDefinitionResult>
{
    public async Task<CreateDefinitionResult> Handle(CreateDefinitionCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<ApprovalType>(request.ApprovalType, out var approvalType))
            approvalType = ApprovalType.Sequential;

        var def = WorkflowDefinition.Create(
            name: request.Name, title: request.Title,
            approvalType: approvalType,
            stepDefinitionsJson: request.StepDefinitionsJson ?? "[]",
            description: request.Description,
            category: request.Category,
            timeoutHours: request.TimeoutHours);

        db.Definitions.Add(def);
        await db.SaveChangesAsync(ct);
        return new CreateDefinitionResult(def.Id, def.Name);
    }
}

// ── Workflow Instance Handlers ───────────────────────────────
public sealed class ListWorkflowsQueryHandler(WorkflowDbContext db)
    : IRequestHandler<ListWorkflowsQuery, PagedResult<WorkflowListItem>>
{
    public async Task<PagedResult<WorkflowListItem>> Handle(ListWorkflowsQuery request, CancellationToken ct)
    {
        var query = db.Instances.Include(i => i.Definition).AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<WorkflowStatus>(request.Status, out var s))
            query = query.Where(i => i.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(i => new WorkflowListItem(i.Id, i.Definition.Name,
                i.Status.ToString(), i.ReferenceType, i.ReferenceId,
                i.CurrentStepOrder, i.StartedAt, i.CompletedAt, i.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WorkflowListItem>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }
}

public sealed class GetWorkflowQueryHandler(WorkflowDbContext db)
    : IRequestHandler<GetWorkflowQuery, object?>
{
    public async Task<object?> Handle(GetWorkflowQuery request, CancellationToken ct)
    {
        var instance = await db.Instances
            .Include(i => i.Definition)
            .Include(i => i.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (instance is null) return null;

        return new
        {
            instance.Id,
            Definition = new { instance.Definition.Name, instance.Definition.Title, ApprovalType = instance.Definition.ApprovalType.ToString() },
            Status = instance.Status.ToString(),
            instance.ReferenceType, instance.ReferenceId, instance.CurrentStepOrder,
            instance.StartedAt, instance.CompletedAt,
            Steps = instance.Steps.Select(s => new
            {
                s.Id, s.StepOrder, s.StepName, Status = s.Status.ToString(),
                s.AssignedUserId, s.AssignedRole, s.ActionByUserId, s.ActionAt, s.Comment, s.DueDate
            })
        };
    }
}

public sealed class GetPendingStepsQueryHandler(IWorkflowEngine engine)
    : IRequestHandler<GetPendingStepsQuery, IReadOnlyList<PendingStepItem>>
{
    public async Task<IReadOnlyList<PendingStepItem>> Handle(GetPendingStepsQuery request, CancellationToken ct)
    {
        var steps = await engine.GetPendingStepsAsync(request.UserId);
        return steps.Select(s => new PendingStepItem(
            s.Id, s.InstanceId, s.StepOrder, s.StepName,
            s.AssignedUserId, s.DueDate, s.CreatedAt)).ToList();
    }
}

// ── Workflow Action Handlers ─────────────────────────────────
public sealed class StartWorkflowCommandHandler(IWorkflowEngine engine)
    : IRequestHandler<StartWorkflowCommand, WorkflowActionResult>
{
    public async Task<WorkflowActionResult> Handle(StartWorkflowCommand request, CancellationToken ct)
    {
        var instance = await engine.StartAsync(request.DefinitionId,
            request.InitiatorUserId, request.ReferenceType, request.ReferenceId, request.Metadata);
        return new WorkflowActionResult(instance.Id, instance.Status.ToString());
    }
}

public sealed class ApproveStepCommandHandler(IWorkflowEngine engine)
    : IRequestHandler<ApproveStepCommand, WorkflowActionResult>
{
    public async Task<WorkflowActionResult> Handle(ApproveStepCommand request, CancellationToken ct)
    {
        var instance = await engine.ApproveAsync(request.InstanceId, request.StepId, request.UserId, request.Comment);
        return new WorkflowActionResult(instance.Id, instance.Status.ToString());
    }
}

public sealed class RejectStepCommandHandler(IWorkflowEngine engine)
    : IRequestHandler<RejectStepCommand, WorkflowActionResult>
{
    public async Task<WorkflowActionResult> Handle(RejectStepCommand request, CancellationToken ct)
    {
        var instance = await engine.RejectAsync(request.InstanceId, request.StepId, request.UserId, request.Comment);
        return new WorkflowActionResult(instance.Id, instance.Status.ToString());
    }
}

public sealed class EscalateStepCommandHandler(IWorkflowEngine engine)
    : IRequestHandler<EscalateStepCommand, WorkflowActionResult>
{
    public async Task<WorkflowActionResult> Handle(EscalateStepCommand request, CancellationToken ct)
    {
        var instance = await engine.EscalateAsync(request.InstanceId, request.StepId, request.EscalateToUserId, request.Comment);
        return new WorkflowActionResult(instance.Id, instance.Status.ToString());
    }
}

public sealed class CancelWorkflowCommandHandler(IWorkflowEngine engine)
    : IRequestHandler<CancelWorkflowCommand, WorkflowActionResult>
{
    public async Task<WorkflowActionResult> Handle(CancelWorkflowCommand request, CancellationToken ct)
    {
        var instance = await engine.CancelAsync(request.InstanceId);
        return new WorkflowActionResult(instance.Id, instance.Status.ToString());
    }
}
