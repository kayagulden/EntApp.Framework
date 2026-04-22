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
