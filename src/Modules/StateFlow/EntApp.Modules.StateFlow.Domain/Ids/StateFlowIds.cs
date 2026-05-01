using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.StateFlow.Domain.Ids;

public readonly record struct StateFlowDefinitionId(Guid Value) : IEntityId;
public readonly record struct StateDefinitionId(Guid Value) : IEntityId;
public readonly record struct TransitionDefinitionId(Guid Value) : IEntityId;
public readonly record struct RuleExecutionLogId(Guid Value) : IEntityId;
public readonly record struct EventAutomationRuleId(Guid Value) : IEntityId;
