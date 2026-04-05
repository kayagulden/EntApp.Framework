using EntApp.Modules.Inventory.Domain.Entities;
using EntApp.Modules.Inventory.Domain.Enums;
using EntApp.Modules.Inventory.Domain.Ids;
using EntApp.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Inventory.Infrastructure.Endpoints;

/// <summary>Inventory REST API endpoint'leri.</summary>
public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Products ═══════════
        var products = app.MapGroup("/api/inventory/products").WithTags("Inventory - Products");

        products.MapGet("/", async (InventoryDbContext db, string? search, string? category,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Products.Where(p => p.IsActive);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search)
                    || (p.Barcode != null && p.Barcode.Contains(search)));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            var total = await query.CountAsync();
            var items = await query.OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new { p.Id, p.SKU, p.Barcode, p.Name, p.Category,
                    ProductType = p.ProductType.ToString(), Unit = p.Unit.ToString(),
                    p.UnitPrice, p.CostPrice, p.MinStock, p.MaxStock, p.ReorderPoint })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListProducts");

        products.MapGet("/{id:guid}", async (Guid id, InventoryDbContext db) =>
        {
            var p = await db.Products.FindAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        }).WithName("GetProduct");

        products.MapPost("/", async (CreateProductRequest req, InventoryDbContext db) =>
        {
            Enum.TryParse<ProductType>(req.ProductType, out var type);
            Enum.TryParse<UnitOfMeasure>(req.Unit, out var unit);
            var product = ProductBase.Create(req.SKU, req.Name, type, unit, req.Barcode,
                req.Description, req.Category, req.UnitPrice, req.CostPrice,
                req.Currency ?? "TRY", req.MinStock, req.MaxStock, req.ReorderPoint);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/inventory/products/{product.Id}",
                new { product.Id, product.SKU });
        }).WithName("CreateProduct");

        // ═══════════ Warehouses ═══════════
        var wh = app.MapGroup("/api/inventory/warehouses").WithTags("Inventory - Warehouses");

        wh.MapGet("/", async (InventoryDbContext db) =>
        {
            var items = await db.Warehouses.OrderBy(w => w.Code)
                .Select(w => new { w.Id, w.Code, w.Name, w.City,
                    Status = w.Status.ToString(), w.ManagerUserId })
                .ToListAsync();
            return Results.Ok(items);
        }).WithName("ListWarehouses");

        wh.MapPost("/", async (CreateWarehouseRequest req, InventoryDbContext db) =>
        {
            var warehouse = WarehouseBase.Create(req.Code, req.Name, req.Address, req.City, req.ManagerUserId);
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync();
            return Results.Created($"/api/inventory/warehouses/{warehouse.Id}",
                new { warehouse.Id, warehouse.Code });
        }).WithName("CreateWarehouse");

        // ═══════════ Stock Movements ═══════════
        var moves = app.MapGroup("/api/inventory/movements").WithTags("Inventory - Stock Movements");

        moves.MapGet("/", async (InventoryDbContext db, Guid? productId, Guid? warehouseId,
            string? type, int page = 1, int pageSize = 20) =>
        {
            var query = db.StockMovements
                .Include(m => m.Product).Include(m => m.Warehouse).AsQueryable();
            if (productId.HasValue) query = query.Where(m => m.ProductId.Value == productId.Value);
            if (warehouseId.HasValue) query = query.Where(m => m.WarehouseId.Value == warehouseId.Value);
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<MovementType>(type, out var mt))
                query = query.Where(m => m.MovementType == mt);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(m => m.MovementDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new { m.Id, ProductName = m.Product.Name, m.Product.SKU,
                    WarehouseName = m.Warehouse.Name,
                    MovementType = m.MovementType.ToString(),
                    m.Quantity, m.UnitCost, m.MovementDate, m.ReferenceNumber })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListStockMovements");

        moves.MapPost("/", async (CreateStockMovementRequest req, InventoryDbContext db) =>
        {
            Enum.TryParse<MovementType>(req.MovementType, out var type);
            var movement = StockMovementBase.Create(new ProductId(req.ProductId), new WarehouseId(req.WarehouseId), type,
                req.Quantity, req.UnitCost, req.MovementDate, req.TargetWarehouseId.HasValue ? new WarehouseId(req.TargetWarehouseId.Value) : null,
                req.ReferenceNumber, req.Notes);
            db.StockMovements.Add(movement);
            await db.SaveChangesAsync();
            return Results.Created($"/api/inventory/movements/{movement.Id}",
                new { movement.Id, MovementType = movement.MovementType.ToString() });
        }).WithName("CreateStockMovement");

        // ═══════════ Stock Balance ═══════════
        var stock = app.MapGroup("/api/inventory/stock").WithTags("Inventory - Stock");

        stock.MapGet("/balance", async (InventoryDbContext db, Guid? warehouseId) =>
        {
            var query = db.StockMovements.Include(m => m.Product).Include(m => m.Warehouse).AsQueryable();
            if (warehouseId.HasValue) query = query.Where(m => m.WarehouseId.Value == warehouseId.Value);

            var balance = await query
                .GroupBy(m => new { m.ProductId, m.Product.SKU, m.Product.Name,
                    m.WarehouseId, WarehouseName = m.Warehouse.Name })
                .Select(g => new
                {
                    g.Key.ProductId, g.Key.SKU, ProductName = g.Key.Name,
                    g.Key.WarehouseId, g.Key.WarehouseName,
                    StockIn = g.Where(m => m.MovementType == MovementType.StockIn
                        || m.MovementType == MovementType.Return)
                        .Sum(m => m.Quantity),
                    StockOut = g.Where(m => m.MovementType == MovementType.StockOut)
                        .Sum(m => m.Quantity),
                    Adjustments = g.Where(m => m.MovementType == MovementType.Adjustment)
                        .Sum(m => m.Quantity)
                })
                .ToListAsync();

            var result = balance.Select(b => new
            {
                b.ProductId, b.SKU, b.ProductName, b.WarehouseId, b.WarehouseName,
                CurrentStock = b.StockIn - b.StockOut + b.Adjustments
            }).OrderBy(r => r.SKU);

            return Results.Ok(result);
        }).WithName("StockBalance").WithSummary("Depo bazlı stok bakiyesi");

        // ── Low Stock Alert ──────────────────────────────
        stock.MapGet("/alerts", async (InventoryDbContext db) =>
        {
            var products = await db.Products.Where(p => p.IsActive && p.ReorderPoint > 0).ToListAsync();
            var movements = await db.StockMovements.ToListAsync();

            var alerts = products.Select(p =>
            {
                var stockIn = movements.Where(m => m.ProductId.Value == p.Id.Value
                    && (m.MovementType == MovementType.StockIn || m.MovementType == MovementType.Return))
                    .Sum(m => m.Quantity);
                var stockOut = movements.Where(m => m.ProductId.Value == p.Id.Value
                    && m.MovementType == MovementType.StockOut).Sum(m => m.Quantity);
                var adj = movements.Where(m => m.ProductId.Value == p.Id.Value
                    && m.MovementType == MovementType.Adjustment).Sum(m => m.Quantity);
                var current = stockIn - stockOut + adj;

                return new { p.Id, p.SKU, p.Name, CurrentStock = current,
                    p.MinStock, p.ReorderPoint, p.MaxStock,
                    IsBelowMin = current < p.MinStock,
                    NeedsReorder = current <= p.ReorderPoint };
            })
            .Where(a => a.IsBelowMin || a.NeedsReorder)
            .OrderBy(a => a.CurrentStock)
            .ToList();

            return Results.Ok(alerts);
        }).WithName("StockAlerts").WithSummary("Min stok/reorder uyarıları");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
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
