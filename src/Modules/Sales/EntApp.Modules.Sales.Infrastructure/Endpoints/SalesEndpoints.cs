using EntApp.Modules.Sales.Application.Commands;
using EntApp.Modules.Sales.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.Sales.Infrastructure.Endpoints;

/// <summary>Sales REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var pl = app.MapGroup("/api/sales/price-lists").WithTags("Sales - Price Lists");
        pl.MapGet("/", async (ISender mediator) => Results.Ok(await mediator.Send(new ListPriceListsQuery()))).WithName("ListPriceLists");
        pl.MapPost("/", async (CreatePriceListRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreatePriceListCommand(req.Code, req.Name, req.ListType, req.Currency, req.ValidFrom, req.ValidTo, req.PriceItemsJson));
            return Results.Created($"/api/sales/price-lists/{id}", new { id });
        }).WithName("CreatePriceList");

        var orders = app.MapGroup("/api/sales/orders").WithTags("Sales - Orders");
        orders.MapGet("/", async (ISender mediator, string? status, Guid? customerId, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListOrdersQuery(status, customerId, page, pageSize)))).WithName("ListOrders");
        orders.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetOrderQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetOrder");
        orders.MapPost("/", async (CreateOrderRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateOrderCommand(req.OrderNumber, req.CustomerId, req.OrderDate,
                req.CustomerName, req.Currency, req.ShippingAddress, req.Notes, req.PriceListId, req.AssignedUserId,
                req.Items?.Select(i => new CreateOrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice,
                    i.TaxRate, i.DiscountType, i.DiscountValue, i.ProductSKU)).ToList()));
            return Results.Created($"/api/sales/orders/{result.Id}", result);
        }).WithName("CreateOrder");

        orders.MapPost("/{id:guid}/confirm", async (Guid id, ISender mediator) =>
        { var s = await mediator.Send(new ConfirmOrderCommand(id)); return Results.Ok(new { id, status = s }); }).WithName("ConfirmOrder");
        orders.MapPost("/{id:guid}/ship", async (Guid id, ShipRequest req, ISender mediator) =>
        { var s = await mediator.Send(new ShipOrderCommand(id, req.ShipDate)); return Results.Ok(new { id, status = s }); }).WithName("ShipOrder");
        orders.MapPost("/{id:guid}/deliver", async (Guid id, ISender mediator) =>
        { var s = await mediator.Send(new DeliverOrderCommand(id)); return Results.Ok(new { id, status = s }); }).WithName("DeliverOrder");
        orders.MapPost("/{id:guid}/cancel", async (Guid id, ISender mediator) =>
        { var s = await mediator.Send(new CancelOrderCommand(id)); return Results.Ok(new { id, status = s }); }).WithName("CancelOrder");

        orders.MapGet("/dashboard", async (ISender mediator) => Results.Ok(await mediator.Send(new GetSalesDashboardQuery())))
            .WithName("SalesDashboard").WithSummary("Satış paneli özeti");

        return app;
    }
}

// ── Request DTO'lar ─────────────────────────────────────────
public sealed record CreatePriceListRequest(string Code, string Name, string ListType = "Standard",
    string? Currency = null, DateTime? ValidFrom = null, DateTime? ValidTo = null, string? PriceItemsJson = null);
public sealed record CreateOrderRequest(string OrderNumber, Guid CustomerId, DateTime OrderDate = default,
    string? CustomerName = null, string? Currency = null, string? ShippingAddress = null,
    string? Notes = null, Guid? PriceListId = null, Guid? AssignedUserId = null, List<CreateOrderItemRequest>? Items = null);
public sealed record CreateOrderItemRequest(Guid ProductId, string ProductName, decimal Quantity = 1,
    decimal UnitPrice = 0, decimal TaxRate = 20, string DiscountType = "Percentage",
    decimal DiscountValue = 0, string? ProductSKU = null);
public sealed record ShipRequest(DateTime? ShipDate = null);
