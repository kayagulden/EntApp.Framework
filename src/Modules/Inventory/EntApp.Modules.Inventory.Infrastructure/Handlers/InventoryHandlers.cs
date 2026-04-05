using EntApp.Modules.Inventory.Application.Commands;
using EntApp.Modules.Inventory.Application.Queries;
using EntApp.Modules.Inventory.Domain.Entities;
using EntApp.Modules.Inventory.Domain.Enums;
using EntApp.Modules.Inventory.Domain.Ids;
using EntApp.Modules.Inventory.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Inventory.Infrastructure.Handlers;

// ── Query Handlers ──────────────────────────────────────────
public sealed class ListProductsQueryHandler(InventoryDbContext db)
    : IRequestHandler<ListProductsQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListProductsQuery request, CancellationToken ct)
    {
        var query = db.Products.Where(p => p.IsActive);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search) || p.SKU.Contains(request.Search)
                || (p.Barcode != null && p.Barcode.Contains(request.Search)));
        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(p => p.Category == request.Category);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => (object)new { p.Id, p.SKU, p.Barcode, p.Name, p.Category,
                ProductType = p.ProductType.ToString(), Unit = p.Unit.ToString(),
                p.UnitPrice, p.CostPrice, p.MinStock, p.MaxStock, p.ReorderPoint })
            .ToListAsync(ct);

        return new PagedResult<object>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetProductQueryHandler(InventoryDbContext db) : IRequestHandler<GetProductQuery, object?>
{
    public async Task<object?> Handle(GetProductQuery request, CancellationToken ct)
        => await db.Products.FindAsync([request.Id], ct);
}

public sealed class ListWarehousesQueryHandler(InventoryDbContext db) : IRequestHandler<ListWarehousesQuery, List<object>>
{
    public async Task<List<object>> Handle(ListWarehousesQuery request, CancellationToken ct)
    {
        return await db.Warehouses.OrderBy(w => w.Code)
            .Select(w => (object)new { w.Id, w.Code, w.Name, w.City, Status = w.Status.ToString(), w.ManagerUserId })
            .ToListAsync(ct);
    }
}

public sealed class ListStockMovementsQueryHandler(InventoryDbContext db)
    : IRequestHandler<ListStockMovementsQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListStockMovementsQuery request, CancellationToken ct)
    {
        var query = db.StockMovements.Include(m => m.Product).Include(m => m.Warehouse).AsQueryable();
        if (request.ProductId.HasValue) query = query.Where(m => m.ProductId.Value == request.ProductId.Value);
        if (request.WarehouseId.HasValue) query = query.Where(m => m.WarehouseId.Value == request.WarehouseId.Value);
        if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<MovementType>(request.Type, out var mt))
            query = query.Where(m => m.MovementType == mt);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(m => m.MovementDate)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(m => (object)new { m.Id, ProductName = m.Product.Name, m.Product.SKU,
                WarehouseName = m.Warehouse.Name, MovementType = m.MovementType.ToString(),
                m.Quantity, m.UnitCost, m.MovementDate, m.ReferenceNumber })
            .ToListAsync(ct);

        return new PagedResult<object>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetStockBalanceQueryHandler(InventoryDbContext db) : IRequestHandler<GetStockBalanceQuery, object>
{
    public async Task<object> Handle(GetStockBalanceQuery request, CancellationToken ct)
    {
        var query = db.StockMovements.Include(m => m.Product).Include(m => m.Warehouse).AsQueryable();
        if (request.WarehouseId.HasValue) query = query.Where(m => m.WarehouseId.Value == request.WarehouseId.Value);

        var balance = await query
            .GroupBy(m => new { m.ProductId, m.Product.SKU, m.Product.Name, m.WarehouseId, WarehouseName = m.Warehouse.Name })
            .Select(g => new
            {
                g.Key.ProductId, g.Key.SKU, ProductName = g.Key.Name,
                g.Key.WarehouseId, g.Key.WarehouseName,
                StockIn = g.Where(m => m.MovementType == MovementType.StockIn || m.MovementType == MovementType.Return).Sum(m => m.Quantity),
                StockOut = g.Where(m => m.MovementType == MovementType.StockOut).Sum(m => m.Quantity),
                Adjustments = g.Where(m => m.MovementType == MovementType.Adjustment).Sum(m => m.Quantity)
            }).ToListAsync(ct);

        return balance.Select(b => new
        {
            b.ProductId, b.SKU, b.ProductName, b.WarehouseId, b.WarehouseName,
            CurrentStock = b.StockIn - b.StockOut + b.Adjustments
        }).OrderBy(r => r.SKU).ToList();
    }
}

public sealed class GetStockAlertsQueryHandler(InventoryDbContext db) : IRequestHandler<GetStockAlertsQuery, object>
{
    public async Task<object> Handle(GetStockAlertsQuery request, CancellationToken ct)
    {
        var products = await db.Products.Where(p => p.IsActive && p.ReorderPoint > 0).ToListAsync(ct);
        var movements = await db.StockMovements.ToListAsync(ct);

        return products.Select(p =>
        {
            var stockIn = movements.Where(m => m.ProductId.Value == p.Id.Value
                && (m.MovementType == MovementType.StockIn || m.MovementType == MovementType.Return)).Sum(m => m.Quantity);
            var stockOut = movements.Where(m => m.ProductId.Value == p.Id.Value
                && m.MovementType == MovementType.StockOut).Sum(m => m.Quantity);
            var adj = movements.Where(m => m.ProductId.Value == p.Id.Value
                && m.MovementType == MovementType.Adjustment).Sum(m => m.Quantity);
            var current = stockIn - stockOut + adj;
            return new { p.Id, p.SKU, p.Name, CurrentStock = current, p.MinStock, p.ReorderPoint, p.MaxStock,
                IsBelowMin = current < p.MinStock, NeedsReorder = current <= p.ReorderPoint };
        }).Where(a => a.IsBelowMin || a.NeedsReorder).OrderBy(a => a.CurrentStock).ToList();
    }
}

// ── Command Handlers ────────────────────────────────────────
public sealed class CreateProductCommandHandler(InventoryDbContext db) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        Enum.TryParse<ProductType>(request.ProductType, out var type);
        Enum.TryParse<UnitOfMeasure>(request.Unit, out var unit);
        var product = ProductBase.Create(request.SKU, request.Name, type, unit, request.Barcode,
            request.Description, request.Category, request.UnitPrice, request.CostPrice,
            request.Currency ?? "TRY", request.MinStock, request.MaxStock, request.ReorderPoint);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id.Value;
    }
}

public sealed class CreateWarehouseCommandHandler(InventoryDbContext db) : IRequestHandler<CreateWarehouseCommand, Guid>
{
    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = WarehouseBase.Create(request.Code, request.Name, request.Address, request.City, request.ManagerUserId);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return warehouse.Id.Value;
    }
}

public sealed class CreateStockMovementCommandHandler(InventoryDbContext db) : IRequestHandler<CreateStockMovementCommand, Guid>
{
    public async Task<Guid> Handle(CreateStockMovementCommand request, CancellationToken ct)
    {
        Enum.TryParse<MovementType>(request.MovementType, out var type);
        var movement = StockMovementBase.Create(new ProductId(request.ProductId), new WarehouseId(request.WarehouseId),
            type, request.Quantity, request.UnitCost, request.MovementDate,
            request.TargetWarehouseId.HasValue ? new WarehouseId(request.TargetWarehouseId.Value) : null,
            request.ReferenceNumber, request.Notes);
        db.StockMovements.Add(movement);
        await db.SaveChangesAsync(ct);
        return movement.Id.Value;
    }
}
