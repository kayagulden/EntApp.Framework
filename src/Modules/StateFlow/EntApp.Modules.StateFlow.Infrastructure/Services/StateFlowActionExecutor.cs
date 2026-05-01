using System.Diagnostics;
using System.Text.Json;
using EntApp.Modules.StateFlow.Domain.Entities;
using EntApp.Modules.StateFlow.Domain.Events;
using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Modules.StateFlow.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.StateFlow.Infrastructure.Services;

/// <summary>
/// StateFlow Action Executor — geçiş sonrası otomatik aksiyonları çalıştırır.
/// StateTransitionCompletedEvent'i dinler, ilgili transition ve hedef state'in
/// aksiyon tanımlarını (OnTransitionActions, OnEntryActions) sırayla execute eder.
/// Recursion prevention: execution context flag ile kendi tetiklediği geçişleri engeller.
/// </summary>
internal sealed class StateFlowActionExecutor : INotificationHandler<StateTransitionCompletedEvent>
{
    private readonly StateFlowDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<StateFlowActionExecutor> _logger;

    // Recursion prevention — aynı thread'de aksiyon executor'ün tetiklediği event'i tekrar çalıştırmasını engeller
    private static readonly AsyncLocal<bool> _isExecuting = new();

    public StateFlowActionExecutor(
        StateFlowDbContext db,
        IMediator mediator,
        ILogger<StateFlowActionExecutor> logger)
    {
        _db = db;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(StateTransitionCompletedEvent notification, CancellationToken ct)
    {
        // Recursion prevention
        if (_isExecuting.Value)
        {
            _logger.LogDebug(
                "Skipping action execution for {EntityType}:{EntityId} — already executing (recursion prevention)",
                notification.EntityType, notification.EntityId);
            return;
        }

        try
        {
            _isExecuting.Value = true;

            var flowId = EntityId.From<StateFlowDefinitionId>(notification.FlowDefinitionId);

            // Akış tanımını yükle (states + transitions dahil)
            var flow = await _db.FlowDefinitions
                .AsNoTracking()
                .Include(f => f.States)
                .Include(f => f.Transitions)
                .FirstOrDefaultAsync(f => f.Id == flowId, ct);

            if (flow is null)
            {
                _logger.LogWarning("Flow definition not found: {FlowId}", notification.FlowDefinitionId);
                return;
            }

            // 1. Transition Actions — hangi transition tetiklendi?
            var transition = flow.Transitions
                .FirstOrDefault(t => t.FromStateName == notification.FromState
                    && t.ToStateName == notification.ToState
                    && t.TriggerName == notification.Trigger);

            if (transition?.OnTransitionActions is not null)
            {
                await ExecuteActionsAsync(
                    flow, notification, "OnTransition",
                    notification.FromState, notification.Trigger,
                    transition.OnTransitionActions, ct);
            }

            // 2. Entry Actions — hedef state'e giriş aksiyonları
            var targetState = flow.States
                .FirstOrDefault(s => s.Name == notification.ToState);

            if (targetState?.OnEntryActions is not null)
            {
                await ExecuteActionsAsync(
                    flow, notification, "OnEntry",
                    notification.ToState, notification.Trigger,
                    targetState.OnEntryActions, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled error executing actions for {EntityType}:{EntityId} transition {From}->{To}",
                notification.EntityType, notification.EntityId,
                notification.FromState, notification.ToState);
        }
        finally
        {
            _isExecuting.Value = false;
        }
    }

    /// <summary>JSON aksiyon listesini parse edip sırayla çalıştırır.</summary>
    private async Task ExecuteActionsAsync(
        StateFlowDefinition flow,
        StateTransitionCompletedEvent evt,
        string source, string stateName, string? triggerName,
        string actionsJson, CancellationToken ct)
    {
        List<ActionDefinition>? actions;
        try
        {
            actions = JsonSerializer.Deserialize<List<ActionDefinition>>(actionsJson, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse actions JSON for flow {FlowId}, state {State}",
                flow.Id.Value, stateName);
            return;
        }

        if (actions is null || actions.Count == 0) return;

        foreach (var action in actions)
        {
            var sw = Stopwatch.StartNew();
            bool success = false;
            string? errorMessage = null;

            try
            {
                await ExecuteSingleActionAsync(action, evt, ct);
                success = true;

                _logger.LogInformation(
                    "Action {ActionType} executed successfully for {EntityType}:{EntityId} ({Source}: {State})",
                    action.Type, evt.EntityType, evt.EntityId, source, stateName);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(ex,
                    "Action {ActionType} failed for {EntityType}:{EntityId} ({Source}: {State})",
                    action.Type, evt.EntityType, evt.EntityId, source, stateName);
            }
            finally
            {
                sw.Stop();

                // Execution log kaydı
                var log = RuleExecutionLog.Create(
                    flow.Id, evt.EntityType, evt.EntityId,
                    source, stateName, triggerName,
                    action.Type,
                    JsonSerializer.Serialize(action.Params, _jsonOptions),
                    success, errorMessage, (int)sw.ElapsedMilliseconds);

                _db.RuleExecutionLogs.Add(log);
            }
        }

        // Tüm log'ları tek seferde kaydet
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Tek bir aksiyonu çalıştırır.</summary>
    private async Task ExecuteSingleActionAsync(
        ActionDefinition action,
        StateTransitionCompletedEvent evt,
        CancellationToken ct)
    {
        switch (action.Type.ToLowerInvariant())
        {
            case "sendnotification":
                await HandleSendNotificationAsync(action, evt, ct);
                break;

            case "addcomment":
                await HandleAddCommentAsync(action, evt, ct);
                break;

            case "assignworkitem":
                await HandleAssignWorkItemAsync(action, evt, ct);
                break;

            case "changestatus":
                await HandleChangeStatusAsync(action, evt, ct);
                break;

            default:
                _logger.LogWarning("Unknown action type: {ActionType}", action.Type);
                break;
        }
    }

    // ── Action Handlers ──────────────────────────────────────────

    private Task HandleSendNotificationAsync(
        ActionDefinition action, StateTransitionCompletedEvent evt, CancellationToken ct)
    {
        // TODO: Integration event publish — Notification modülüne
        // Şimdilik log ile kaydediyoruz
        var channel = GetParam(action, "channel", "InApp");
        var template = GetParam(action, "template", "state_transition");
        var recipientRole = GetParam(action, "recipientRole", "Assignee");

        _logger.LogInformation(
            "→ SendNotification: channel={Channel}, template={Template}, recipient={Recipient}, entity={EntityType}:{EntityId}",
            channel, template, recipientRole, evt.EntityType, evt.EntityId);

        return Task.CompletedTask;
    }

    private Task HandleAddCommentAsync(
        ActionDefinition action, StateTransitionCompletedEvent evt, CancellationToken ct)
    {
        // TODO: MediatR command dispatch — CreateCommentCommand
        var content = GetParam(action, "content", $"Durum otomatik olarak '{evt.ToState}' olarak güncellendi.");

        _logger.LogInformation(
            "→ AddComment: content=\"{Content}\", entity={EntityType}:{EntityId}",
            content, evt.EntityType, evt.EntityId);

        return Task.CompletedTask;
    }

    private Task HandleAssignWorkItemAsync(
        ActionDefinition action, StateTransitionCompletedEvent evt, CancellationToken ct)
    {
        // TODO: MediatR command dispatch — AssignWorkItemCommand
        var userId = GetParam(action, "userId", "");
        var role = GetParam(action, "role", "");

        _logger.LogInformation(
            "→ AssignWorkItem: userId={UserId}, role={Role}, entity={EntityType}:{EntityId}",
            userId, role, evt.EntityType, evt.EntityId);

        return Task.CompletedTask;
    }

    private Task HandleChangeStatusAsync(
        ActionDefinition action, StateTransitionCompletedEvent evt, CancellationToken ct)
    {
        // TODO: Dikkatli kullanılmalı — recursion prevention aktif olduğundan
        // bu aksiyon kendi entity'sinde çalışmaz, ilişkili entity'lerde kullanılır
        var targetStatus = GetParam(action, "status", "");
        var targetEntityType = GetParam(action, "targetEntityType", "");

        _logger.LogInformation(
            "→ ChangeStatus: status={Status}, targetEntityType={TargetType}, entity={EntityType}:{EntityId}",
            targetStatus, targetEntityType, evt.EntityType, evt.EntityId);

        return Task.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static string GetParam(ActionDefinition action, string key, string defaultValue)
    {
        if (action.Params is null) return defaultValue;
        return action.Params.TryGetValue(key, out var val) ? val?.ToString() ?? defaultValue : defaultValue;
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Aksiyon JSON yapısı.</summary>
    internal sealed class ActionDefinition
    {
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object?>? Params { get; set; }
    }
}
