using EntApp.Modules.Sales.Domain.Entities;
using EntApp.Modules.Sales.Domain.Enums;
using EntApp.Modules.Sales.Application.IntegrationEvents;
using EntApp.Modules.Sales.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Sales.Infrastructure.Endpoints;

/// <summary>Sales REST API endpoint'leri.</summary>
public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Price Lists ═══════════
        var pl = app.MapGroup("/api/sales/price-lists").WithTags("Sales - Price Lists");

        pl.MapGet("/", async (SalesDbContext db) =>
        {
            var items = await db.PriceLists.Where(p => p.IsActive).OrderBy(p => p.Code)
                .Select(p => new { p.Id, p.Code, p.Name, ListType = p.ListType.ToString(),
                    p.Currency, p.ValidFrom, p.ValidTo })
                .ToListAsync();
            return Results.Ok(items);
        }).WithName("ListPriceLists");

        pl.MapPost("/", async (CreatePriceListRequest req, SalesDbContext db) =>
        {
            Enum.TryParse<PriceListType>(req.ListType, out var type);
            var priceList = PriceListBase.Create(req.Code, req.Name, type,
                req.Currency ?? "TRY", req.ValidFrom, req.ValidTo, req.PriceItemsJson);
            db.PriceLists.Add(priceList);
            await db.SaveChangesAsync();
            return Results.Created($"/api/sales/price-lists/{priceList.Id}",
                new { priceList.Id, priceList.Code });
        }).WithName("CreatePriceList");

        // ═══════════ Orders ═══════════
        var orders = app.MapGroup("/api/sales/orders").WithTags("Sales - Orders");

        orders.MapGet("/", async (SalesDbContext db, string? status, Guid? customerId,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Orders.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
                query = query.Where(o => o.Status == s);
            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new { o.Id, o.OrderNumber, o.CustomerName,
                    Status = o.Status.ToString(), o.OrderDate, o.GrandTotal,
                    o.Currency, ItemCount = o.Items.Count })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListOrders");

        orders.MapGet("/{id:guid}", async (Guid id, SalesDbContext db) =>
        {
            var order = await db.Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).WithName("GetOrder");

        orders.MapPost("/", async (CreateOrderRequest req, SalesDbContext db) =>
        {
            var order = SalesOrderBase.Create(req.OrderNumber, req.CustomerId,
                req.OrderDate == default ? DateTime.UtcNow : req.OrderDate,
                req.CustomerName, req.Currency ?? "TRY",
                req.ShippingAddress, req.Notes, req.PriceListId, req.AssignedUserId);

            foreach (var item in req.Items ?? [])
            {
                Enum.TryParse<DiscountType>(item.DiscountType, out var dt);
                var orderItem = OrderItemBase.Create(order.Id, item.ProductId,
                    item.ProductName, item.Quantity, item.UnitPrice,
                    item.TaxRate, dt, item.DiscountValue, item.ProductSKU);
                order.Items.Add(orderItem);
            }

            order.Recalculate();
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return Results.Created($"/api/sales/orders/{order.Id}",
                new { order.Id, order.OrderNumber, order.GrandTotal });
        }).WithName("CreateOrder");

        // ── Status transitions ───────────────────────────
        orders.MapPost("/{id:guid}/confirm", async (Guid id, SalesDbContext db, IEventBus eventBus) =>
        {
            var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();
            order.Confirm();
            await db.SaveChangesAsync();

            // Integration Event: Inventory stok düşümü + Finance fatura oluşturma
            await eventBus.PublishAsync(new OrderConfirmedEvent(
                order.Id, order.OrderNumber, order.CustomerId, order.CustomerName,
                order.Currency, order.GrandTotal,
                order.Items.Select(i => new OrderConfirmedEvent.OrderLineDto(
                    i.ProductId, i.ProductName, i.ProductSKU,
                    i.Quantity, i.UnitPrice, i.TaxRate,
                    i.LineTotal, i.TaxAmount, i.DiscountAmount)).ToList()));

            return Results.Ok(new { order.Id, Status = order.Status.ToString() });
        }).WithName("ConfirmOrder");

        orders.MapPost("/{id:guid}/ship", async (Guid id, ShipRequest req, SalesDbContext db) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();
            order.Ship(req.ShipDate ?? DateTime.UtcNow);
            await db.SaveChangesAsync();
            return Results.Ok(new { order.Id, Status = order.Status.ToString() });
        }).WithName("ShipOrder");

        orders.MapPost("/{id:guid}/deliver", async (Guid id, SalesDbContext db) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();
            order.Deliver(DateTime.UtcNow);
            await db.SaveChangesAsync();
            return Results.Ok(new { order.Id, Status = order.Status.ToString() });
        }).WithName("DeliverOrder");

        orders.MapPost("/{id:guid}/cancel", async (Guid id, SalesDbContext db, IEventBus eventBus) =>
        {
            var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();
            order.Cancel();
            await db.SaveChangesAsync();

            // Integration Event: Inventory stok iadesi
            await eventBus.PublishAsync(new OrderCancelledEvent(
                order.Id, order.OrderNumber,
                order.Items.Select(i => new OrderCancelledEvent.CancelledLineDto(
                    i.ProductId, i.Quantity)).ToList()));

            return Results.Ok(new { order.Id, Status = order.Status.ToString() });
        }).WithName("CancelOrder");

        // ── Dashboard ────────────────────────────────────
        orders.MapGet("/dashboard", async (SalesDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var summary = new
            {
                TodayOrders = await db.Orders.CountAsync(o => o.OrderDate >= today),
                TodayRevenue = await db.Orders.Where(o => o.OrderDate >= today
                    && o.Status != OrderStatus.Cancelled).SumAsync(o => o.GrandTotal),
                MonthOrders = await db.Orders.CountAsync(o => o.OrderDate >= monthStart),
                MonthRevenue = await db.Orders.Where(o => o.OrderDate >= monthStart
                    && o.Status != OrderStatus.Cancelled).SumAsync(o => o.GrandTotal),
                ByStatus = await db.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync()
            };

            return Results.Ok(summary);
        }).WithName("SalesDashboard").WithSummary("Satış paneli özeti");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
public sealed record CreatePriceListRequest(string Code, string Name,
    string ListType = "Standard", string? Currency = null,
    DateTime? ValidFrom = null, DateTime? ValidTo = null, string? PriceItemsJson = null);

public sealed record CreateOrderRequest(string OrderNumber, Guid CustomerId,
    DateTime OrderDate = default, string? CustomerName = null, string? Currency = null,
    string? ShippingAddress = null, string? Notes = null,
    Guid? PriceListId = null, Guid? AssignedUserId = null,
    List<CreateOrderItemDto>? Items = null);

public sealed record CreateOrderItemDto(Guid ProductId, string ProductName,
    decimal Quantity = 1, decimal UnitPrice = 0, decimal TaxRate = 20,
    string DiscountType = "Percentage", decimal DiscountValue = 0, string? ProductSKU = null);

public sealed record ShipRequest(DateTime? ShipDate = null);
