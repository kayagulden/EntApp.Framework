using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.Sales.Application.IntegrationEvents;

/// <summary>Sipariş onaylandığında yayınlanır → Inventory stok düşer, Finance fatura oluşur.</summary>
public sealed record OrderConfirmedEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string? CustomerName,
    string Currency,
    decimal GrandTotal,
    IReadOnlyList<OrderConfirmedEvent.OrderLineDto> Lines) : IntegrationEvent
{
    public sealed record OrderLineDto(
        Guid ProductId,
        string ProductName,
        string? ProductSKU,
        decimal Quantity,
        decimal UnitPrice,
        decimal TaxRate,
        decimal LineTotal,
        decimal TaxAmount,
        decimal DiscountAmount);
}

/// <summary>Sipariş iptal edildiğinde yayınlanır → Inventory stok iadesi.</summary>
public sealed record OrderCancelledEvent(
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<OrderCancelledEvent.CancelledLineDto> Lines) : IntegrationEvent
{
    public sealed record CancelledLineDto(Guid ProductId, decimal Quantity);
}

/// <summary>Sipariş sevk edildiğinde yayınlanır.</summary>
public sealed record OrderShippedEvent(
    Guid OrderId,
    string OrderNumber,
    DateTime ShipDate) : IntegrationEvent;
