using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.KnowledgeBase.Domain.Ids;

public readonly record struct WikiSpaceId(Guid Value) : IEntityId;
public readonly record struct WikiPageId(Guid Value) : IEntityId;
public readonly record struct WikiPageVersionId(Guid Value) : IEntityId;
