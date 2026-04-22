using EntApp.Modules.StateFlow.Application.Dtos;
using EntApp.Modules.StateFlow.Application.Interfaces;
using EntApp.Modules.StateFlow.Domain.Entities;
using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Modules.StateFlow.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Stateless;

namespace EntApp.Modules.StateFlow.Infrastructure.Services;

/// <summary>
/// Stateless kütüphanesi ile state machine runtime.
/// DB'den flow tanımı yükler (cache'li), in-memory makine kurar,
/// geçiş validate eder ve tetikler.
/// </summary>
public sealed class StateFlowEngine : IStateFlowEngine
{
    private readonly StateFlowDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public StateFlowEngine(StateFlowDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<bool> ValidateTransitionAsync(
        string entityType, string currentState, string trigger,
        Guid flowDefinitionId, CancellationToken ct = default)
    {
        var flow = await LoadFlowAsync(flowDefinitionId, ct);
        if (flow is null) return false;

        var machine = BuildMachine(flow, currentState);
        return machine.CanFire(trigger);
    }

    public async Task<IReadOnlyList<TriggerInfo>> GetAllowedTriggersAsync(
        string entityType, string currentState,
        Guid flowDefinitionId, CancellationToken ct = default)
    {
        var flow = await LoadFlowAsync(flowDefinitionId, ct);
        if (flow is null) return [];

        var machine = BuildMachine(flow, currentState);
        var permitted = await machine.GetPermittedTriggersAsync();

        return flow.Transitions
            .Where(t => t.FromStateName == currentState && permitted.Contains(t.TriggerName))
            .OrderBy(t => t.SortOrder)
            .Select(t => new TriggerInfo(t.TriggerName, t.Label, t.ToStateName, t.RequiredRole))
            .ToList();
    }

    public async Task<string> FireTransitionAsync(
        string entityType, string currentState, string trigger,
        Guid flowDefinitionId, CancellationToken ct = default)
    {
        var flow = await LoadFlowAsync(flowDefinitionId, ct);
        if (flow is null)
            throw new InvalidOperationException($"Flow definition not found: {flowDefinitionId}");

        var machine = BuildMachine(flow, currentState);

        if (!machine.CanFire(trigger))
            throw new InvalidOperationException(
                $"Transition '{trigger}' is not permitted from state '{currentState}' in flow '{flow.Name}' v{flow.Version}.");

        machine.Fire(trigger);
        return machine.State;
    }

    /// <summary>DB'den flow tanımını yükler — IMemoryCache ile 5 dk cache.</summary>
    private async Task<FlowSnapshot?> LoadFlowAsync(Guid flowDefinitionId, CancellationToken ct)
    {
        var cacheKey = $"stateflow:{flowDefinitionId}";

        if (_cache.TryGetValue(cacheKey, out FlowSnapshot? cached))
            return cached;

        var id = EntityId.From<StateFlowDefinitionId>(flowDefinitionId);

        var flow = await _db.FlowDefinitions
            .AsNoTracking()
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (flow is null) return null;

        var snapshot = new FlowSnapshot(
            flow.Id.Value, flow.EntityType, flow.Name, flow.Version,
            flow.States.Select(s => new StateSnapshot(s.Name, s.IsInitial, s.IsTerminal)).ToList(),
            flow.Transitions.Select(t => new TransitionSnapshot(
                t.FromStateName, t.ToStateName, t.TriggerName, t.Label, t.RequiredRole, t.SortOrder)).ToList());

        _cache.Set(cacheKey, snapshot, CacheDuration);
        return snapshot;
    }

    /// <summary>Stateless kütüphanesi ile in-memory state machine oluşturur.</summary>
    private static StateMachine<string, string> BuildMachine(FlowSnapshot flow, string currentState)
    {
        var machine = new StateMachine<string, string>(currentState);

        // Tüm state'leri kaydet (Stateless'ta zorunlu değil ama guard'lar için faydalı)
        var stateNames = flow.States.Select(s => s.Name).ToHashSet();

        // Transition'ları grupla: (FromState, Trigger) → ToState
        foreach (var group in flow.Transitions.GroupBy(t => new { t.FromStateName, t.TriggerName }))
        {
            var from = group.Key.FromStateName;
            var trigger = group.Key.TriggerName;

            foreach (var t in group)
            {
                machine.Configure(from)
                    .Permit(trigger, t.ToStateName);
            }
        }

        // Tanımlanmamış state'ler için boş konfigürasyon (Stateless hata fırlatmasın)
        foreach (var stateName in stateNames)
        {
            // Configure idempotent — zaten configure edilmiş state'ler etkilenmez
            machine.Configure(stateName);
        }

        return machine;
    }

    // ── Internal cache DTOs ──────────────────────────────────

    private sealed record FlowSnapshot(
        Guid Id, string EntityType, string Name, int Version,
        List<StateSnapshot> States,
        List<TransitionSnapshot> Transitions);

    private sealed record StateSnapshot(string Name, bool IsInitial, bool IsTerminal);

    private sealed record TransitionSnapshot(
        string FromStateName, string ToStateName, string TriggerName,
        string Label, string? RequiredRole, int SortOrder);
}
