using EntApp.Modules.Inventory.Application.Commands;
using EntApp.Modules.Inventory.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.Inventory.Infrastructure.Endpoints;

/// <summary>Inventory REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/inventory/products").WithTags("Inventory - Products");
        products.MapGet("/", async (ISender mediator, string? search, string? category, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListProductsQuery(search, category, page, pageSize)))).WithName("ListProducts");
        products.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetProductQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetProduct");
        products.MapPost("/", async (CreateProductRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateProductCommand(req.SKU, req.Name, req.ProductType, req.Unit,
                req.Barcode, req.Description, req.Category, req.UnitPrice, req.CostPrice, req.Currency, req.MinStock, req.MaxStock, req.ReorderPoint));
            return Results.Created($"/api/inventory/products/{id}", new { id });
        }).WithName("CreateProduct");

        var wh = app.MapGroup("/api/inventory/warehouses").WithTags("Inventory - Warehouses");
        wh.MapGet("/", async (ISender mediator) => Results.Ok(await mediator.Send(new ListWarehousesQuery()))).WithName("ListWarehouses");
        wh.MapPost("/", async (CreateWarehouseRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateWarehouseCommand(req.Code, req.Name, req.Address, req.City, req.ManagerUserId));
            return Results.Created($"/api/inventory/warehouses/{id}", new { id });
        }).WithName("CreateWarehouse");

        var moves = app.MapGroup("/api/inventory/movements").WithTags("Inventory - Stock Movements");
        moves.MapGet("/", async (ISender mediator, Guid? productId, Guid? warehouseId, string? type, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListStockMovementsQuery(productId, warehouseId, type, page, pageSize)))).WithName("ListStockMovements");
        moves.MapPost("/", async (CreateStockMovementRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateStockMovementCommand(req.ProductId, req.WarehouseId, req.MovementType,
                req.Quantity, req.UnitCost, req.MovementDate, req.TargetWarehouseId, req.ReferenceNumber, req.Notes));
            return Results.Created($"/api/inventory/movements/{id}", new { id });
        }).WithName("CreateStockMovement");

        var stock = app.MapGroup("/api/inventory/stock").WithTags("Inventory - Stock");
        stock.MapGet("/balance", async (ISender mediator, Guid? warehouseId)
            => Results.Ok(await mediator.Send(new GetStockBalanceQuery(warehouseId)))).WithName("StockBalance").WithSummary("Depo bazlı stok bakiyesi");
        stock.MapGet("/alerts", async (ISender mediator)
            => Results.Ok(await mediator.Send(new GetStockAlertsQuery()))).WithName("StockAlerts").WithSummary("Min stok/reorder uyarıları");

        return app;
    }
}

// ── Request DTO'lar ─────────────────────────────────────────
public sealed record CreateProductRequest(string SKU, string Name, string ProductType = "Physical",
    string Unit = "Piece", string? Barcode = null, string? Description = null,
    string? Category = null, decimal UnitPrice = 0, decimal CostPrice = 0,
    string? Currency = null, decimal MinStock = 0, decimal MaxStock = 0, decimal ReorderPoint = 0);
public sealed record CreateWarehouseRequest(string Code, string Name,
    string? Address = null, string? City = null, Guid? ManagerUserId = null);
public sealed record CreateStockMovementRequest(Guid ProductId, Guid WarehouseId,
    string MovementType = "StockIn", decimal Quantity = 0, decimal UnitCost = 0,
    DateTime? MovementDate = null, Guid? TargetWarehouseId = null,
    string? ReferenceNumber = null, string? Notes = null);
