using EntApp.Modules.Sales.Domain.Enums;
using EntApp.Modules.Sales.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Sales.Domain.Entities;

/// <summary>Satış siparişi.</summary>
[DynamicEntity("SalesOrder", MenuGroup = "Satış")]
public sealed class SalesOrderBase : AggregateRoot<SalesOrderId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string OrderNumber { get; private set; } = string.Empty;

    /// <summary>Müşteri (CRM modülü referans)</summary>
    public Guid CustomerId { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? CustomerName { get; private set; }

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    public DateTime OrderDate { get; private set; }
    public DateTime? ShipDate { get; private set; }
    public DateTime? DeliveryDate { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal GrandTotal { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? ShippingAddress { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 1000)]
    public string? Notes { get; private set; }

    /// <summary>Fiyat listesi referans</summary>
    public Guid? PriceListId { get; private set; }

    /// <summary>Workflow instance (onay akışı)</summary>
    public Guid? WorkflowInstanceId { get; private set; }

    /// <summary>Sorumlu satış temsilcisi</summary>
    public Guid? AssignedUserId { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    [DynamicDetail(typeof(OrderItemBase))]
    public ICollection<OrderItemBase> Items { get; private set; } = [];

    private SalesOrderBase() { }

    public static SalesOrderBase Create(string orderNumber, Guid customerId,
        DateTime orderDate, string? customerName = null, string currency = "TRY",
        string? shippingAddress = null, string? notes = null,
        Guid? priceListId = null, Guid? assignedUserId = null)
    {
        return new SalesOrderBase
        {
            Id = EntityId.New<SalesOrderId>(), OrderNumber = orderNumber,
            CustomerId = customerId, CustomerName = customerName,
            OrderDate = orderDate, Currency = currency,
            ShippingAddress = shippingAddress, Notes = notes,
            PriceListId = priceListId, AssignedUserId = assignedUserId
        };
    }

    public void Recalculate()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TaxTotal = Items.Sum(i => i.TaxAmount);
        DiscountTotal = Items.Sum(i => i.DiscountAmount);
        GrandTotal = SubTotal + TaxTotal - DiscountTotal;
    }

    public void Confirm() => Status = OrderStatus.Confirmed;
    public void Process() => Status = OrderStatus.Processing;
    public void Ship(DateTime shipDate) { Status = OrderStatus.Shipped; ShipDate = shipDate; }
    public void Deliver(DateTime deliveryDate) { Status = OrderStatus.Delivered; DeliveryDate = deliveryDate; }
    public void Cancel() => Status = OrderStatus.Cancelled;
    public void LinkWorkflow(Guid workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;
}
