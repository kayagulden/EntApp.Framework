using EntApp.Modules.StateFlow.Application.Dtos;
using MediatR;

namespace EntApp.Modules.StateFlow.Application.Commands;

// ── Flow Definition ──────────────────────────────────────────

/// <summary>Yeni akış tanımı oluşturur (Draft).</summary>
public sealed record CreateFlowDefinitionCommand(
    string EntityType, string Key, string Name,
    string? Description = null,
    bool IsGlobalTemplate = false) : IRequest<Guid>;

/// <summary>Akış tanımının temel bilgilerini günceller (sadece Draft).</summary>
public sealed record UpdateFlowDefinitionCommand(
    Guid Id, string Name, string? Description) : IRequest;

/// <summary>Draft akışı yayınlar — önceki Published → Archived.</summary>
public sealed record PublishFlowCommand(Guid FlowDefinitionId) : IRequest;

/// <summary>Yayınlanmış akışı arşivler.</summary>
public sealed record ArchiveFlowCommand(Guid FlowDefinitionId) : IRequest;

/// <summary>Mevcut Published veya Archived akıştan yeni Draft versiyon oluşturur.</summary>
public sealed record CreateNewVersionCommand(Guid SourceFlowDefinitionId) : IRequest<Guid>;

/// <summary>Global şablondan tenant'a özel kopya oluşturur.</summary>
public sealed record CloneFromTemplateCommand(
    Guid TemplateFlowDefinitionId,
    string? CustomName = null) : IRequest<Guid>;

/// <summary>Designer'dan gelen toplu state/transition güncellemesi.</summary>
public sealed record SaveFlowDesignCommand(
    Guid FlowDefinitionId,
    IReadOnlyList<StateDto> States,
    IReadOnlyList<TransitionDto> Transitions) : IRequest;

/// <summary>Akış tanımını siler (sadece Draft).</summary>
public sealed record DeleteFlowDefinitionCommand(Guid FlowDefinitionId) : IRequest;

// ── Engine ───────────────────────────────────────────────────

/// <summary>State geçişi tetikler — entity'nin durumunu değiştirir.</summary>
public sealed record FireTransitionCommand(
    string EntityType,
    string CurrentState,
    string Trigger,
    Guid FlowDefinitionId) : IRequest<string>;

// ── Event Automation Rules ──────────────────────────────────

/// <summary>Yeni event-driven otomasyon kuralı oluşturur.</summary>
public sealed record CreateEventRuleCommand(
    string Name, string TriggerType, string ActionType,
    string? Description = null, string? TriggerConditions = null,
    string? ActionParams = null, string? EntityType = null,
    int Priority = 0, int SortOrder = 0) : IRequest<Guid>;

/// <summary>Event-driven otomasyon kuralını günceller.</summary>
public sealed record UpdateEventRuleCommand(
    Guid Id, string Name, string TriggerType, string ActionType,
    string? Description, string? TriggerConditions,
    string? ActionParams, string? EntityType,
    int Priority, int SortOrder) : IRequest;

/// <summary>Event kuralını aktif/pasif yapar.</summary>
public sealed record ToggleEventRuleCommand(Guid Id) : IRequest;

/// <summary>Event kuralını siler.</summary>
public sealed record DeleteEventRuleCommand(Guid Id) : IRequest;
