using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.CRM.Domain.Ids;

public readonly record struct OpportunityId(Guid Value) : IEntityId;
