using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Inventory.Domain.Ids;

public readonly record struct ProductId(Guid Value) : IEntityId;
public readonly record struct WarehouseId(Guid Value) : IEntityId;
public readonly record struct StockMovementId(Guid Value) : IEntityId;
