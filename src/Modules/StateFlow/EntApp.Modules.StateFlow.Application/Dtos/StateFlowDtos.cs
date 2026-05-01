namespace EntApp.Modules.StateFlow.Application.Dtos;

/// <summary>Akış tanımı listesi DTO'su.</summary>
public sealed record FlowDefinitionDto(
    Guid Id,
    string EntityType,
    string Key,
    string Name,
    string? Description,
    int Version,
    string Status,
    DateTime? PublishedAt,
    bool IsGlobalTemplate,
    Guid? SourceTemplateId,
    int StateCount,
    int TransitionCount,
    DateTime CreatedAt);

/// <summary>Akış tanımı detay DTO'su — state'ler ve transition'lar dahil.</summary>
public sealed record FlowDefinitionDetailDto(
    Guid Id,
    string EntityType,
    string Key,
    string Name,
    string? Description,
    int Version,
    string Status,
    DateTime? PublishedAt,
    bool IsGlobalTemplate,
    Guid? SourceTemplateId,
    DateTime CreatedAt,
    IReadOnlyList<StateDto> States,
    IReadOnlyList<TransitionDto> Transitions);

/// <summary>State tanımı DTO'su.</summary>
public sealed record StateDto(
    Guid Id,
    string Name,
    string Label,
    string Color,
    string? Icon,
    bool IsInitial,
    bool IsTerminal,
    bool IsPaused,
    string Category,
    double PositionX,
    double PositionY,
    int SortOrder,
    string? OnEntryActions);

/// <summary>Geçiş tanımı DTO'su.</summary>
public sealed record TransitionDto(
    Guid Id,
    string FromStateName,
    string ToStateName,
    string TriggerName,
    string Label,
    string? RequiredRole,
    string? GuardExpression,
    int SortOrder,
    string? OnTransitionActions);

/// <summary>İzin verilen tetikleyici bilgisi.</summary>
public sealed record TriggerInfo(
    string TriggerName,
    string Label,
    string ToStateName,
    string? RequiredRole);

/// <summary>Kural çalışma kaydı DTO'su.</summary>
public sealed record RuleExecutionLogDto(
    Guid Id,
    Guid FlowDefinitionId,
    string EntityType,
    Guid TargetEntityId,
    string Source,
    string StateName,
    string? TriggerName,
    string ActionType,
    string ActionParamsJson,
    bool Success,
    string? ErrorMessage,
    int DurationMs,
    DateTime CreatedAt);

/// <summary>Event-driven otomasyon kuralı DTO'su.</summary>
public sealed record EventAutomationRuleDto(
    Guid Id,
    string Name,
    string? Description,
    string TriggerType,
    string TriggerConditions,
    string ActionType,
    string ActionParams,
    string? EntityType,
    bool IsEnabled,
    int Priority,
    int SortOrder,
    DateTime CreatedAt);
