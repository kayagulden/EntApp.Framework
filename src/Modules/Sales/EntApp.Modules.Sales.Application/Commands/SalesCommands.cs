using MediatR;

namespace EntApp.Modules.Sales.Application.Commands;

public sealed record CreatePriceListCommand(string Code, string Name, string ListType = "Standard",
    string? Currency = null, DateTime? ValidFrom = null, DateTime? ValidTo = null, string? PriceItemsJson = null) : IRequest<Guid>;

public sealed record CreateOrderCommand(string OrderNumber, Guid CustomerId, DateTime OrderDate = default,
    string? CustomerName = null, string? Currency = null, string? ShippingAddress = null,
    string? Notes = null, Guid? PriceListId = null, Guid? AssignedUserId = null,
    List<CreateOrderItemDto>? Items = null) : IRequest<CreateOrderResult>;
public sealed record CreateOrderItemDto(Guid ProductId, string ProductName, decimal Quantity = 1,
    decimal UnitPrice = 0, decimal TaxRate = 20, string DiscountType = "Percentage",
    decimal DiscountValue = 0, string? ProductSKU = null);
public sealed record CreateOrderResult(Guid Id, string OrderNumber, decimal GrandTotal);

public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest<string>;
public sealed record ShipOrderCommand(Guid OrderId, DateTime? ShipDate = null) : IRequest<string>;
public sealed record DeliverOrderCommand(Guid OrderId) : IRequest<string>;
public sealed record CancelOrderCommand(Guid OrderId) : IRequest<string>;
