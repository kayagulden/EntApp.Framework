using EntApp.Modules.StateFlow.Application.Commands;
using EntApp.Modules.StateFlow.Application.Dtos;
using EntApp.Modules.StateFlow.Application.Interfaces;
using EntApp.Modules.StateFlow.Application.Queries;
using EntApp.Modules.StateFlow.Domain.Entities;
using EntApp.Modules.StateFlow.Domain.Enums;
using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Modules.StateFlow.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EntApp.Modules.StateFlow.Infrastructure.Handlers;

// ═══════════════════════════════════════════════════════════════
//  COMMAND HANDLERS
// ═══════════════════════════════════════════════════════════════

/// <summary>Yeni akış tanımı oluşturur (Draft).</summary>
internal sealed class CreateFlowDefinitionHandler(StateFlowDbContext db)
    : IRequestHandler<CreateFlowDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(CreateFlowDefinitionCommand request, CancellationToken ct)
    {
        var flow = StateFlowDefinition.Create(
            request.EntityType, request.Key, request.Name,
            request.Description, request.IsGlobalTemplate);

        db.FlowDefinitions.Add(flow);
        await db.SaveChangesAsync(ct);
        return flow.Id.Value;
    }
}

/// <summary>Akış tanımının temel bilgilerini günceller (sadece Draft).</summary>
internal sealed class UpdateFlowDefinitionHandler(StateFlowDbContext db)
    : IRequestHandler<UpdateFlowDefinitionCommand>
{
    public async Task Handle(UpdateFlowDefinitionCommand request, CancellationToken ct)
    {
        var id = EntityId.From<StateFlowDefinitionId>(request.Id);
        var flow = await db.FlowDefinitions.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new InvalidOperationException($"Flow definition not found: {request.Id}");

        flow.Update(request.Name, request.Description);
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Draft akışı yayınlar — önceki Published → Archived.</summary>
internal sealed class PublishFlowHandler(StateFlowDbContext db, IMemoryCache cache)
    : IRequestHandler<PublishFlowCommand>
{
    public async Task Handle(PublishFlowCommand request, CancellationToken ct)
    {
        var id = EntityId.From<StateFlowDefinitionId>(request.FlowDefinitionId);
        var flow = await db.FlowDefinitions
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new InvalidOperationException($"Flow definition not found: {request.FlowDefinitionId}");

        if (flow.Status != FlowStatus.Draft)
            throw new InvalidOperationException("Only Draft flows can be published.");

        // Validasyon: en az 1 initial state, en az 1 terminal state olmalı
        if (!flow.States.Any(s => s.IsInitial))
            throw new InvalidOperationException("Flow must have at least one initial state.");
        if (!flow.States.Any(s => s.IsTerminal))
            throw new InvalidOperationException("Flow must have at least one terminal state.");

        // Önceki Published akışı Archived yap
        var previousPublished = await db.FlowDefinitions
            .FirstOrDefaultAsync(f => f.EntityType == flow.EntityType
                && f.Status == FlowStatus.Published
                && f.TenantId == flow.TenantId
                && f.Id != id, ct);

        if (previousPublished is not null)
        {
            previousPublished.Archive();
            // Önceki versiyonun cache'ini invalidate et
            cache.Remove($"stateflow:{previousPublished.Id.Value}");
        }

        flow.Publish();
        await db.SaveChangesAsync(ct);

        // Yeni Published'ın cache'ini invalidate et (taze yüklensin)
        cache.Remove($"stateflow:{flow.Id.Value}");
    }
}

/// <summary>Yayınlanmış akışı arşivler.</summary>
internal sealed class ArchiveFlowHandler(StateFlowDbContext db, IMemoryCache cache)
    : IRequestHandler<ArchiveFlowCommand>
{
    public async Task Handle(ArchiveFlowCommand request, CancellationToken ct)
    {
        var id = EntityId.From<StateFlowDefinitionId>(request.FlowDefinitionId);
        var flow = await db.FlowDefinitions.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new InvalidOperationException($"Flow definition not found: {request.FlowDefinitionId}");

        flow.Archive();
        await db.SaveChangesAsync(ct);

        cache.Remove($"stateflow:{flow.Id.Value}");
    }
}

/// <summary>Mevcut akıştan yeni Draft versiyon oluşturur (state + transition kopyası).</summary>
internal sealed class CreateNewVersionHandler(StateFlowDbContext db)
    : IRequestHandler<CreateNewVersionCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewVersionCommand request, CancellationToken ct)
    {
        var sourceId = EntityId.From<StateFlowDefinitionId>(request.SourceFlowDefinitionId);
        var source = await db.FlowDefinitions
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .FirstOrDefaultAsync(f => f.Id == sourceId, ct)
            ?? throw new InvalidOperationException($"Source flow definition not found: {request.SourceFlowDefinitionId}");

        var newFlow = StateFlowDefinition.CreateNewVersion(source);
        db.FlowDefinitions.Add(newFlow);

        // State'leri kopyala
        foreach (var state in source.States)
        {
            var cloned = state.Clone(newFlow.Id);
            db.StateDefinitions.Add(cloned);
        }

        // Transition'ları kopyala
        foreach (var transition in source.Transitions)
        {
            var cloned = transition.Clone(newFlow.Id);
            db.TransitionDefinitions.Add(cloned);
        }

        await db.SaveChangesAsync(ct);
        return newFlow.Id.Value;
    }
}

/// <summary>Global şablondan tenant'a özel kopya oluşturur.</summary>
internal sealed class CloneFromTemplateHandler(StateFlowDbContext db)
    : IRequestHandler<CloneFromTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CloneFromTemplateCommand request, CancellationToken ct)
    {
        var templateId = EntityId.From<StateFlowDefinitionId>(request.TemplateFlowDefinitionId);
        var template = await db.FlowDefinitions
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .FirstOrDefaultAsync(f => f.Id == templateId, ct)
            ?? throw new InvalidOperationException($"Template flow definition not found: {request.TemplateFlowDefinitionId}");

        if (!template.IsGlobalTemplate)
            throw new InvalidOperationException("Only global templates can be cloned.");

        var clone = StateFlowDefinition.CloneFromTemplate(template, request.CustomName);
        db.FlowDefinitions.Add(clone);

        foreach (var state in template.States)
        {
            db.StateDefinitions.Add(state.Clone(clone.Id));
        }

        foreach (var transition in template.Transitions)
        {
            db.TransitionDefinitions.Add(transition.Clone(clone.Id));
        }

        await db.SaveChangesAsync(ct);
        return clone.Id.Value;
    }
}

/// <summary>Designer'dan gelen toplu state/transition güncellemesi.</summary>
internal sealed class SaveFlowDesignHandler(StateFlowDbContext db, IMemoryCache cache)
    : IRequestHandler<SaveFlowDesignCommand>
{
    public async Task Handle(SaveFlowDesignCommand request, CancellationToken ct)
    {
        var flowId = EntityId.From<StateFlowDefinitionId>(request.FlowDefinitionId);
        var flow = await db.FlowDefinitions
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .FirstOrDefaultAsync(f => f.Id == flowId, ct)
            ?? throw new InvalidOperationException($"Flow definition not found: {request.FlowDefinitionId}");

        if (flow.Status != FlowStatus.Draft)
            throw new InvalidOperationException("Only Draft flows can be edited.");

        // ── States: upsert + delete ─────────────────────────
        var existingStates = flow.States.ToDictionary(s => s.Id.Value);
        var incomingStateIds = request.States.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToHashSet();

        // Silinecek state'ler
        foreach (var existing in existingStates.Values)
        {
            if (!incomingStateIds.Contains(existing.Id.Value))
                db.StateDefinitions.Remove(existing);
        }

        // Eklenecek / güncellenecek state'ler
        foreach (var dto in request.States)
        {
            if (dto.Id != Guid.Empty && existingStates.TryGetValue(dto.Id, out var existing))
            {
                existing.Update(dto.Label, dto.Color, dto.Icon,
                    dto.IsInitial, dto.IsTerminal, dto.IsPaused,
                    dto.Category, dto.PositionX, dto.PositionY,
                    dto.SortOrder, dto.OnEntryActions);
            }
            else
            {
                var newState = StateDefinition.Create(
                    flowId, dto.Name, dto.Label,
                    dto.Color, dto.Icon,
                    dto.IsInitial, dto.IsTerminal, dto.IsPaused,
                    dto.Category, dto.PositionX, dto.PositionY,
                    dto.SortOrder, dto.OnEntryActions);
                db.StateDefinitions.Add(newState);
            }
        }

        // ── Transitions: upsert + delete ────────────────────
        var existingTransitions = flow.Transitions.ToDictionary(t => t.Id.Value);
        var incomingTransitionIds = request.Transitions.Where(t => t.Id != Guid.Empty).Select(t => t.Id).ToHashSet();

        foreach (var existing in existingTransitions.Values)
        {
            if (!incomingTransitionIds.Contains(existing.Id.Value))
                db.TransitionDefinitions.Remove(existing);
        }

        foreach (var dto in request.Transitions)
        {
            if (dto.Id != Guid.Empty && existingTransitions.TryGetValue(dto.Id, out var existing))
            {
                existing.Update(dto.FromStateName, dto.ToStateName,
                    dto.TriggerName, dto.Label,
                    dto.RequiredRole, dto.GuardExpression, dto.SortOrder,
                    dto.OnTransitionActions);
            }
            else
            {
                var newTransition = TransitionDefinition.Create(
                    flowId, dto.FromStateName, dto.ToStateName,
                    dto.TriggerName, dto.Label,
                    dto.RequiredRole, dto.GuardExpression, dto.SortOrder,
                    dto.OnTransitionActions);
                db.TransitionDefinitions.Add(newTransition);
            }
        }

        await db.SaveChangesAsync(ct);

        // Cache invalidate
        cache.Remove($"stateflow:{flow.Id.Value}");
    }
}

/// <summary>Akış tanımını siler (sadece Draft).</summary>
internal sealed class DeleteFlowDefinitionHandler(StateFlowDbContext db)
    : IRequestHandler<DeleteFlowDefinitionCommand>
{
    public async Task Handle(DeleteFlowDefinitionCommand request, CancellationToken ct)
    {
        var id = EntityId.From<StateFlowDefinitionId>(request.FlowDefinitionId);
        var flow = await db.FlowDefinitions.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new InvalidOperationException($"Flow definition not found: {request.FlowDefinitionId}");

        if (flow.Status != FlowStatus.Draft)
            throw new InvalidOperationException("Only Draft flows can be deleted.");

        flow.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>State geçişi tetikler.</summary>
internal sealed class FireTransitionHandler(IStateFlowEngine engine)
    : IRequestHandler<FireTransitionCommand, string>
{
    public async Task<string> Handle(FireTransitionCommand request, CancellationToken ct)
    {
        return await engine.FireTransitionAsync(
            request.EntityType, request.CurrentState,
            request.Trigger, request.FlowDefinitionId, ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  QUERY HANDLERS
// ═══════════════════════════════════════════════════════════════

/// <summary>Akış tanımlarını listeler.</summary>
internal sealed class ListFlowDefinitionsHandler(StateFlowDbContext db)
    : IRequestHandler<ListFlowDefinitionsQuery, IReadOnlyList<FlowDefinitionDto>>
{
    public async Task<IReadOnlyList<FlowDefinitionDto>> Handle(
        ListFlowDefinitionsQuery request, CancellationToken ct)
    {
        var query = db.FlowDefinitions
            .AsNoTracking()
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(f => f.EntityType == request.EntityType);

        if (!request.IncludeArchived)
            query = query.Where(f => f.Status != FlowStatus.Archived);

        var flows = await query
            .OrderBy(f => f.EntityType)
            .ThenByDescending(f => f.Version)
            .ToListAsync(ct);

        return flows.Select(f => new FlowDefinitionDto(
            f.Id.Value, f.EntityType, f.Key, f.Name, f.Description,
            f.Version, f.Status.ToString(), f.PublishedAt,
            f.IsGlobalTemplate, f.SourceTemplateId,
            f.States.Count, f.Transitions.Count,
            f.CreatedAt)).ToList();
    }
}

/// <summary>Akış tanımını detaylı getirir.</summary>
internal sealed class GetFlowDefinitionHandler(StateFlowDbContext db)
    : IRequestHandler<GetFlowDefinitionQuery, FlowDefinitionDetailDto?>
{
    public async Task<FlowDefinitionDetailDto?> Handle(
        GetFlowDefinitionQuery request, CancellationToken ct)
    {
        var id = EntityId.From<StateFlowDefinitionId>(request.Id);
        var flow = await db.FlowDefinitions
            .AsNoTracking()
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        return flow is null ? null : HandlerMapper.MapToDetailDto(flow);
    }
}

/// <summary>Belirtilen entity tipi için Published akışı getirir.</summary>
internal sealed class GetPublishedFlowHandler(StateFlowDbContext db)
    : IRequestHandler<GetPublishedFlowQuery, FlowDefinitionDetailDto?>
{
    public async Task<FlowDefinitionDetailDto?> Handle(
        GetPublishedFlowQuery request, CancellationToken ct)
    {
        var flow = await db.FlowDefinitions
            .AsNoTracking()
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .FirstOrDefaultAsync(f => f.EntityType == request.EntityType
                && f.Status == FlowStatus.Published, ct);

        return flow is null ? null : HandlerMapper.MapToDetailDto(flow);
    }
}

/// <summary>İzin verilen geçişleri getirir.</summary>
internal sealed class GetAllowedTriggersHandler(IStateFlowEngine engine)
    : IRequestHandler<GetAllowedTriggersQuery, IReadOnlyList<TriggerInfo>>
{
    public async Task<IReadOnlyList<TriggerInfo>> Handle(
        GetAllowedTriggersQuery request, CancellationToken ct)
    {
        return await engine.GetAllowedTriggersAsync(
            request.EntityType, request.CurrentState,
            request.FlowDefinitionId, ct);
    }
}

/// <summary>Geçiş validasyonu.</summary>
internal sealed class ValidateTransitionHandler(IStateFlowEngine engine)
    : IRequestHandler<ValidateTransitionQuery, bool>
{
    public async Task<bool> Handle(ValidateTransitionQuery request, CancellationToken ct)
    {
        return await engine.ValidateTransitionAsync(
            request.EntityType, request.CurrentState,
            request.Trigger, request.FlowDefinitionId, ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  HELPER
// ═══════════════════════════════════════════════════════════════

internal static class HandlerMapper
{
    public static FlowDefinitionDetailDto MapToDetailDto(StateFlowDefinition flow)
    {
        return new FlowDefinitionDetailDto(
            flow.Id.Value, flow.EntityType, flow.Key, flow.Name,
            flow.Description, flow.Version, flow.Status.ToString(),
            flow.PublishedAt, flow.IsGlobalTemplate, flow.SourceTemplateId,
            flow.CreatedAt,
            flow.States.OrderBy(s => s.SortOrder).Select(s => new StateDto(
                s.Id.Value, s.Name, s.Label, s.Color, s.Icon,
                s.IsInitial, s.IsTerminal, s.IsPaused, s.Category,
                s.PositionX, s.PositionY, s.SortOrder, s.OnEntryActions)).ToList(),
            flow.Transitions.OrderBy(t => t.SortOrder).Select(t => new TransitionDto(
                t.Id.Value, t.FromStateName, t.ToStateName,
                t.TriggerName, t.Label, t.RequiredRole,
                t.GuardExpression, t.SortOrder, t.OnTransitionActions)).ToList());
    }
}

/// <summary>Aksiyon çalışma geçmişini getirir.</summary>
internal sealed class ListRuleExecutionLogsHandler(StateFlowDbContext db)
    : IRequestHandler<ListRuleExecutionLogsQuery, IReadOnlyList<RuleExecutionLogDto>>
{
    public async Task<IReadOnlyList<RuleExecutionLogDto>> Handle(
        ListRuleExecutionLogsQuery request, CancellationToken ct)
    {
        var query = db.RuleExecutionLogs.AsNoTracking().AsQueryable();

        if (request.FlowDefinitionId.HasValue)
        {
            var flowId = EntityId.From<StateFlowDefinitionId>(request.FlowDefinitionId.Value);
            query = query.Where(l => l.FlowDefinitionId == flowId);
        }

        if (request.EntityId.HasValue)
            query = query.Where(l => l.TargetEntityId == request.EntityId.Value);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(request.Limit)
            .ToListAsync(ct);

        return logs.Select(l => new RuleExecutionLogDto(
            l.Id.Value, l.FlowDefinitionId.Value,
            l.EntityType, l.TargetEntityId,
            l.Source, l.StateName, l.TriggerName,
            l.ActionType, l.ActionParamsJson,
            l.Success, l.ErrorMessage, l.DurationMs,
            l.CreatedAt)).ToList();
    }
}

