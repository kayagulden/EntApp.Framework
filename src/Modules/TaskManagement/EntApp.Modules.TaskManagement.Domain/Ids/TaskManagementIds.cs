using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.TaskManagement.Domain.Ids;

public readonly record struct PortfolioId(Guid Value) : IEntityId;
public readonly record struct ProjectId(Guid Value) : IEntityId;
public readonly record struct ConfigurationItemId(Guid Value) : IEntityId;
public readonly record struct WorkItemId(Guid Value) : IEntityId;
public readonly record struct CommentId(Guid Value) : IEntityId;
public readonly record struct TimeEntryId(Guid Value) : IEntityId;
public readonly record struct ProjectDeliverableId(Guid Value) : IEntityId;
public readonly record struct CIRelationshipId(Guid Value) : IEntityId;
public readonly record struct SprintId(Guid Value) : IEntityId;
public readonly record struct BoardColumnId(Guid Value) : IEntityId;
public readonly record struct BurndownSnapshotId(Guid Value) : IEntityId;
public readonly record struct MilestoneId(Guid Value) : IEntityId;
