using MediatR;

namespace EntApp.Modules.Inventory.Application.Commands;

public sealed record CreateProductCommand(string SKU, string Name, string ProductType = "Physical",
    string Unit = "Piece", string? Barcode = null, string? Description = null,
    string? Category = null, decimal UnitPrice = 0, decimal CostPrice = 0,
    string? Currency = null, decimal MinStock = 0, decimal MaxStock = 0, decimal ReorderPoint = 0) : IRequest<Guid>;

public sealed record CreateWarehouseCommand(string Code, string Name,
    string? Address = null, string? City = null, Guid? ManagerUserId = null) : IRequest<Guid>;

public sealed record CreateStockMovementCommand(Guid ProductId, Guid WarehouseId,
    string MovementType = "StockIn", decimal Quantity = 0, decimal UnitCost = 0,
    DateTime? MovementDate = null, Guid? TargetWarehouseId = null,
    string? ReferenceNumber = null, string? Notes = null) : IRequest<Guid>;
