using EntApp.Modules.Inventory.Domain.Entities;
using EntApp.Modules.Inventory.Domain.Enums;
using EntApp.Modules.Inventory.Infrastructure.Persistence;
using EntApp.Modules.Procurement.Application.IntegrationEvents;
using EntApp.Modules.Sales.Application.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Inventory.Infrastructure.EventHandlers;

/// <summary>
/// Sipariş onaylandığında → stok düşümü yapar.
/// Varsayılan depodan (ilk aktif depo) StockOut oluşturur.
/// </summary>
public sealed class OrderConfirmedStockHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<OrderConfirmedStockHandler> _logger;

    public OrderConfirmedStockHandler(InventoryDbContext db, ILogger<OrderConfirmedStockHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.Status == WarehouseStatus.Active, cancellationToken);
        if (warehouse is null)
        {
            _logger.LogWarning("OrderConfirmedStockHandler: Aktif depo bulunamadı. OrderId={OrderId}", notification.OrderId);
            return;
        }

        foreach (var line in notification.Lines)
        {
            var movement = StockMovementBase.Create(
                line.ProductId, warehouse.Id,
                MovementType.StockOut, line.Quantity,
                unitCost: 0, referenceNumber: notification.OrderNumber);
            _db.StockMovements.Add(movement);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("OrderConfirmedStockHandler: {LineCount} kalem stok düşüldü. OrderId={OrderId}",
            notification.Lines.Count, notification.OrderId);
    }
}

/// <summary>
/// Sipariş iptal edildiğinde → stok iadesi yapar.
/// </summary>
public sealed class OrderCancelledStockHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<OrderCancelledStockHandler> _logger;

    public OrderCancelledStockHandler(InventoryDbContext db, ILogger<OrderCancelledStockHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.Status == WarehouseStatus.Active, cancellationToken);
        if (warehouse is null) return;

        foreach (var line in notification.Lines)
        {
            var movement = StockMovementBase.Create(
                line.ProductId, warehouse.Id,
                MovementType.Return, line.Quantity,
                referenceNumber: $"CANCEL-{notification.OrderNumber}");
            _db.StockMovements.Add(movement);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("OrderCancelledStockHandler: {LineCount} kalem stok iade edildi. OrderId={OrderId}",
            notification.Lines.Count, notification.OrderId);
    }
}

/// <summary>
/// PO teslim alındığında → stok girişi yapar.
/// </summary>
public sealed class GoodsReceivedStockHandler : INotificationHandler<GoodsReceivedEvent>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<GoodsReceivedStockHandler> _logger;

    public GoodsReceivedStockHandler(InventoryDbContext db, ILogger<GoodsReceivedStockHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(GoodsReceivedEvent notification, CancellationToken cancellationToken)
    {
        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.Status == WarehouseStatus.Active, cancellationToken);
        if (warehouse is null)
        {
            _logger.LogWarning("GoodsReceivedStockHandler: Aktif depo bulunamadı. PO={POId}", notification.PurchaseOrderId);
            return;
        }

        foreach (var line in notification.Lines)
        {
            var movement = StockMovementBase.Create(
                line.ProductId, warehouse.Id,
                MovementType.StockIn, line.Quantity,
                unitCost: line.UnitCost, referenceNumber: notification.OrderNumber);
            _db.StockMovements.Add(movement);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("GoodsReceivedStockHandler: {LineCount} kalem stok girişi yapıldı. PO={POId}",
            notification.Lines.Count, notification.PurchaseOrderId);
    }
}
