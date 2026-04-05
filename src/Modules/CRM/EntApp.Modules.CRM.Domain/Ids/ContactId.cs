using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.CRM.Domain.Ids;

public readonly record struct ContactId(Guid Value) : IEntityId;
