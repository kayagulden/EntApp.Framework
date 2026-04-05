using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Sales.Domain.Ids;

public readonly record struct PriceListId(Guid Value) : IEntityId;
