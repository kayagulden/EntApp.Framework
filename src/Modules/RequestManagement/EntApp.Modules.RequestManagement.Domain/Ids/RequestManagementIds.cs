using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.RequestManagement.Domain.Ids;

public readonly record struct DepartmentId(Guid Value) : IEntityId;
public readonly record struct RequestCategoryId(Guid Value) : IEntityId;
public readonly record struct SlaDefinitionId(Guid Value) : IEntityId;
public readonly record struct TicketId(Guid Value) : IEntityId;
public readonly record struct TicketCommentId(Guid Value) : IEntityId;
public readonly record struct TicketStatusHistoryId(Guid Value) : IEntityId;
