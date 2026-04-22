using EntApp.Modules.StateFlow.Application.Dtos;
using MediatR;

namespace EntApp.Modules.StateFlow.Application.Queries;

/// <summary>Akış tanımlarını listeler (opsiyonel EntityType filtresi).</summary>
public sealed record ListFlowDefinitionsQuery(
    string? EntityType = null,
    bool IncludeArchived = false) : IRequest<IReadOnlyList<FlowDefinitionDto>>;

/// <summary>Akış tanımını detaylı getirir (state + transition dahil).</summary>
public sealed record GetFlowDefinitionQuery(Guid Id) : IRequest<FlowDefinitionDetailDto?>;

/// <summary>Belirtilen entity tipi için Published akışı getirir.</summary>
public sealed record GetPublishedFlowQuery(string EntityType) : IRequest<FlowDefinitionDetailDto?>;

/// <summary>Belirtilen entity ve state için izin verilen geçişleri getirir.</summary>
public sealed record GetAllowedTriggersQuery(
    string EntityType,
    string CurrentState,
    Guid FlowDefinitionId) : IRequest<IReadOnlyList<TriggerInfo>>;

/// <summary>Belirtilen geçişin uygulanabilir olup olmadığını kontrol eder.</summary>
public sealed record ValidateTransitionQuery(
    string EntityType,
    string CurrentState,
    string Trigger,
    Guid FlowDefinitionId) : IRequest<bool>;
