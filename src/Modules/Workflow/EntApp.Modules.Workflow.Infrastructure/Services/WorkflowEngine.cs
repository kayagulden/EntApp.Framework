using System.Text.Json;
using EntApp.Modules.Workflow.Application.Interfaces;
using EntApp.Modules.Workflow.Domain.Entities;
using EntApp.Modules.Workflow.Domain.Enums;
using EntApp.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Workflow.Infrastructure.Services;

/// <summary>
/// Workflow motoru — state machine tabanlı onay akış yönetimi.
/// </summary>
public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly WorkflowDbContext _db;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(WorkflowDbContext db, ILogger<WorkflowEngine> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<WorkflowInstance> StartAsync(
        Guid definitionId, Guid? initiatorUserId = null,
        string? referenceType = null, string? referenceId = null,
        string? metadata = null, CancellationToken ct = default)
    {
        var definition = await _db.Definitions.FindAsync([definitionId], ct)
            ?? throw new KeyNotFoundException($"Workflow definition '{definitionId}' not found.");

        if (!definition.IsActive)
            throw new InvalidOperationException("Workflow definition is not active.");

        // Instance oluştur
        var instance = WorkflowInstance.Create(
            definitionId, initiatorUserId, referenceType, referenceId, metadata);

        _db.Instances.Add(instance);

        // Adım tanımlarını parse et ve step'leri oluştur
        var stepDefs = JsonSerializer.Deserialize<List<StepDefinitionDto>>(
            definition.StepDefinitionsJson) ?? [];

        foreach (var stepDef in stepDefs)
        {
            var step = ApprovalStep.Create(
                instanceId: instance.Id,
                stepOrder: stepDef.Order,
                stepName: stepDef.Name,
                assignedUserId: stepDef.AssignedUserId,
                assignedRole: stepDef.AssignedRole,
                dueDate: definition.TimeoutHours.HasValue
                    ? DateTime.UtcNow.AddHours(definition.TimeoutHours.Value)
                    : null,
                escalationUserId: stepDef.EscalationUserId);

            _db.ApprovalSteps.Add(step);
        }

        instance.Start();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[WF] Started instance {Id} from definition '{Name}'",
            instance.Id, definition.Name);

        return instance;
    }

    public async Task<WorkflowInstance> ApproveAsync(
        Guid instanceId, Guid stepId, Guid userId,
        string? comment = null, CancellationToken ct = default)
    {
        var instance = await GetInstanceWithStepsAsync(instanceId, ct);
        ValidateActiveInstance(instance);

        var step = instance.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new KeyNotFoundException($"Step '{stepId}' not found.");

        if (step.Status != StepStatus.Pending)
            throw new InvalidOperationException($"Step is already '{step.Status}'.");

        step.Approve(userId, comment);

        _logger.LogInformation("[WF] Step '{Step}' approved by {UserId} in instance {Instance}",
            step.StepName, userId, instanceId);

        // Sonraki adıma geç veya tamamla
        await AdvanceWorkflowAsync(instance, ct);

        return instance;
    }

    public async Task<WorkflowInstance> RejectAsync(
        Guid instanceId, Guid stepId, Guid userId,
        string? comment = null, CancellationToken ct = default)
    {
        var instance = await GetInstanceWithStepsAsync(instanceId, ct);
        ValidateActiveInstance(instance);

        var step = instance.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new KeyNotFoundException($"Step '{stepId}' not found.");

        if (step.Status != StepStatus.Pending)
            throw new InvalidOperationException($"Step is already '{step.Status}'.");

        step.Reject(userId, comment);
        instance.Reject();

        // Kalan pending adımları atla
        foreach (var pendingStep in instance.Steps.Where(s => s.Status == StepStatus.Pending))
        {
            pendingStep.Skip("Workflow rejected.");
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[WF] Instance {Id} rejected at step '{Step}' by {UserId}",
            instanceId, step.StepName, userId);

        return instance;
    }

    public async Task<WorkflowInstance> EscalateAsync(
        Guid instanceId, Guid stepId,
        Guid escalateToUserId, string? comment = null, CancellationToken ct = default)
    {
        var instance = await GetInstanceWithStepsAsync(instanceId, ct);
        ValidateActiveInstance(instance);

        var step = instance.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new KeyNotFoundException($"Step '{stepId}' not found.");

        step.Escalate(escalateToUserId, comment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[WF] Step '{Step}' escalated to {UserId} in instance {Instance}",
            step.StepName, escalateToUserId, instanceId);

        return instance;
    }

    public async Task<WorkflowInstance> CancelAsync(Guid instanceId, CancellationToken ct = default)
    {
        var instance = await GetInstanceWithStepsAsync(instanceId, ct);
        ValidateActiveInstance(instance);

        instance.Cancel();

        foreach (var step in instance.Steps.Where(s => s.Status == StepStatus.Pending))
        {
            step.Skip("Workflow cancelled.");
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[WF] Instance {Id} cancelled", instanceId);
        return instance;
    }

    public async Task<IReadOnlyList<ApprovalStep>> GetPendingStepsAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _db.ApprovalSteps
            .Include(s => s.Instance)
            .Where(s => s.Status == StepStatus.Pending && s.AssignedUserId == userId)
            .OrderBy(s => s.DueDate)
            .ToListAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────

    private async Task<WorkflowInstance> GetInstanceWithStepsAsync(Guid instanceId, CancellationToken ct)
    {
        return await _db.Instances
            .Include(i => i.Steps)
            .Include(i => i.Definition)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new KeyNotFoundException($"Workflow instance '{instanceId}' not found.");
    }

    private static void ValidateActiveInstance(WorkflowInstance instance)
    {
        if (instance.Status != WorkflowStatus.Active)
            throw new InvalidOperationException($"Workflow is '{instance.Status}', expected 'Active'.");
    }

    private async Task AdvanceWorkflowAsync(WorkflowInstance instance, CancellationToken ct)
    {
        var definition = instance.Definition;
        var steps = instance.Steps.OrderBy(s => s.StepOrder).ToList();

        switch (definition.ApprovalType)
        {
            case ApprovalType.Sequential:
                var currentStep = steps.FirstOrDefault(s => s.StepOrder == instance.CurrentStepOrder);
                if (currentStep is not null && currentStep.Status == StepStatus.Approved)
                {
                    var nextStep = steps.FirstOrDefault(s => s.StepOrder > instance.CurrentStepOrder
                        && s.Status == StepStatus.Pending);
                    if (nextStep is not null)
                    {
                        instance.AdvanceStep();
                    }
                    else
                    {
                        instance.Complete();
                    }
                }
                break;

            case ApprovalType.Parallel:
                if (steps.All(s => s.Status == StepStatus.Approved))
                {
                    instance.Complete();
                }
                break;

            case ApprovalType.AnyOne:
                if (steps.Any(s => s.Status == StepStatus.Approved))
                {
                    instance.Complete();
                    foreach (var remaining in steps.Where(s => s.Status == StepStatus.Pending))
                    {
                        remaining.Skip("Approved by another approver.");
                    }
                }
                break;
        }

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Adım tanımı DTO — JSON'dan deserialize edilir.</summary>
internal sealed class StepDefinitionDto
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public string? AssignedRole { get; set; }
    public Guid? EscalationUserId { get; set; }
}
