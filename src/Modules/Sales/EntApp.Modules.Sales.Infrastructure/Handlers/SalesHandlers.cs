using EntApp.Modules.Sales.Application.Commands;
using EntApp.Modules.Sales.Application.IntegrationEvents;
using EntApp.Modules.Sales.Application.Queries;
using EntApp.Modules.Sales.Domain.Entities;
using EntApp.Modules.Sales.Domain.Enums;
using EntApp.Modules.Sales.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Contracts.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Sales.Infrastructure.Handlers;

// ── Queries ─────────────────────────────────────────────────
public sealed class ListPriceListsQueryHandler(SalesDbContext db) : IRequestHandler<ListPriceListsQuery, List<object>>
{
    public async Task<List<object>> Handle(ListPriceListsQuery request, CancellationToken ct)
        => await db.PriceLists.Where(p => p.IsActive).OrderBy(p => p.Code)
            .Select(p => (object)new { p.Id, p.Code, p.Name, ListType = p.ListType.ToString(), p.Currency, p.ValidFrom, p.ValidTo })
            .ToListAsync(ct);
}

public sealed class ListOrdersQueryHandler(SalesDbContext db) : IRequestHandler<ListOrdersQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListOrdersQuery request, CancellationToken ct)
    {
        var query = db.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<OrderStatus>(request.Status, out var s))
            query = query.Where(o => o.Status == s);
        if (request.CustomerId.HasValue) query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.OrderDate)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(o => (object)new { o.Id, o.OrderNumber, o.CustomerName, Status = o.Status.ToString(),
                o.OrderDate, o.GrandTotal, o.Currency, ItemCount = o.Items.Count })
            .ToListAsync(ct);

        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetOrderQueryHandler(SalesDbContext db) : IRequestHandler<GetOrderQuery, object?>
{
    public async Task<object?> Handle(GetOrderQuery request, CancellationToken ct)
        => await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id.Value == request.Id, ct);
}

public sealed class GetSalesDashboardQueryHandler(SalesDbContext db) : IRequestHandler<GetSalesDashboardQuery, object>
{
    public async Task<object> Handle(GetSalesDashboardQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new
        {
            TodayOrders = await db.Orders.CountAsync(o => o.OrderDate >= today, ct),
            TodayRevenue = await db.Orders.Where(o => o.OrderDate >= today && o.Status != OrderStatus.Cancelled).SumAsync(o => o.GrandTotal, ct),
            MonthOrders = await db.Orders.CountAsync(o => o.OrderDate >= monthStart, ct),
            MonthRevenue = await db.Orders.Where(o => o.OrderDate >= monthStart && o.Status != OrderStatus.Cancelled).SumAsync(o => o.GrandTotal, ct),
            ByStatus = await db.Orders.GroupBy(o => o.Status).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct)
        };
    }
}

// ── Commands ────────────────────────────────────────────────
public sealed class CreatePriceListCommandHandler(SalesDbContext db) : IRequestHandler<CreatePriceListCommand, Guid>
{
    public async Task<Guid> Handle(CreatePriceListCommand request, CancellationToken ct)
    {
        Enum.TryParse<PriceListType>(request.ListType, out var type);
        var priceList = PriceListBase.Create(request.Code, request.Name, type,
            request.Currency ?? "TRY", request.ValidFrom, request.ValidTo, request.PriceItemsJson);
        db.PriceLists.Add(priceList);
        await db.SaveChangesAsync(ct);
        return priceList.Id.Value;
    }
}

public sealed class CreateOrderCommandHandler(SalesDbContext db) : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = SalesOrderBase.Create(request.OrderNumber, request.CustomerId,
            request.OrderDate == default ? DateTime.UtcNow : request.OrderDate,
            request.CustomerName, request.Currency ?? "TRY",
            request.ShippingAddress, request.Notes, request.PriceListId, request.AssignedUserId);

        foreach (var item in request.Items ?? [])
        {
            Enum.TryParse<DiscountType>(item.DiscountType, out var dt);
            var orderItem = OrderItemBase.Create(order.Id, item.ProductId, item.ProductName,
                item.Quantity, item.UnitPrice, item.TaxRate, dt, item.DiscountValue, item.ProductSKU);
            order.Items.Add(orderItem);
        }

        order.Recalculate();
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return new CreateOrderResult(order.Id.Value, order.OrderNumber, order.GrandTotal);
    }
}

public sealed class ConfirmOrderCommandHandler(SalesDbContext db, IEventBus eventBus) : IRequestHandler<ConfirmOrderCommand, string>
{
    public async Task<string> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id.Value == request.OrderId, ct)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");
        order.Confirm();
        await db.SaveChangesAsync(ct);
        await eventBus.PublishAsync(new OrderConfirmedEvent(order.Id.Value, order.OrderNumber, order.CustomerId,
            order.CustomerName, order.Currency, order.GrandTotal,
            order.Items.Select(i => new OrderConfirmedEvent.OrderLineDto(i.ProductId, i.ProductName, i.ProductSKU,
                i.Quantity, i.UnitPrice, i.TaxRate, i.LineTotal, i.TaxAmount, i.DiscountAmount)).ToList()));
        return order.Status.ToString();
    }
}

public sealed class ShipOrderCommandHandler(SalesDbContext db) : IRequestHandler<ShipOrderCommand, string>
{
    public async Task<string> Handle(ShipOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([request.OrderId], ct)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");
        order.Ship(request.ShipDate ?? DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return order.Status.ToString();
    }
}

public sealed class DeliverOrderCommandHandler(SalesDbContext db) : IRequestHandler<DeliverOrderCommand, string>
{
    public async Task<string> Handle(DeliverOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([request.OrderId], ct)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");
        order.Deliver(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return order.Status.ToString();
    }
}

public sealed class CancelOrderCommandHandler(SalesDbContext db, IEventBus eventBus) : IRequestHandler<CancelOrderCommand, string>
{
    public async Task<string> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id.Value == request.OrderId, ct)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");
        order.Cancel();
        await db.SaveChangesAsync(ct);
        await eventBus.PublishAsync(new OrderCancelledEvent(order.Id.Value, order.OrderNumber,
            order.Items.Select(i => new OrderCancelledEvent.CancelledLineDto(i.ProductId, i.Quantity)).ToList()));
        return order.Status.ToString();
    }
}
