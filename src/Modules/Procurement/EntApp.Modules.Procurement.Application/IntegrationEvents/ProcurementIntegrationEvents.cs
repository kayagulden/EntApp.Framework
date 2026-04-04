using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.Procurement.Application.IntegrationEvents;

/// <summary>Satın alma siparişi teslim alındığında yayınlanır → Inventory stok girişi.</summary>
public sealed record GoodsReceivedEvent(
    Guid PurchaseOrderId,
    string OrderNumber,
    Guid SupplierId,
    string? SupplierName,
    IReadOnlyList<GoodsReceivedEvent.ReceivedLineDto> Lines) : IntegrationEvent
{
    public sealed record ReceivedLineDto(
        Guid ProductId,
        decimal Quantity,
        decimal UnitCost);
}
