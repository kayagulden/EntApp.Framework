using EntApp.Modules.Sales.Domain.Enums;
using EntApp.Modules.Sales.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Sales.Domain.Entities;

/// <summary>Sipariş kalemi.</summary>
public sealed class OrderItemBase : AuditableEntity<OrderItemId>, ITenantEntity
{
    public SalesOrderId OrderId { get; private set; }

    /// <summary>Ürün referans (Inventory modülü)</summary>
    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;
    public string? ProductSKU { get; private set; }

    public decimal Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; } = 20;

    public DiscountType DiscountType { get; private set; } = DiscountType.Percentage;
    public decimal DiscountValue { get; private set; }

    /// <summary>Quantity × UnitPrice</summary>
    public decimal LineTotal { get; private set; }

    /// <summary>LineTotal × TaxRate / 100</summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>Hesaplanan iskonto tutarı</summary>
    public decimal DiscountAmount { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public SalesOrderBase Order { get; private set; } = null!;

    private OrderItemBase() { }

    public static OrderItemBase Create(SalesOrderId orderId, Guid productId,
        string productName, decimal quantity, decimal unitPrice,
        decimal taxRate = 20, DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 0, string? productSKU = null)
    {
        var lineTotal = quantity * unitPrice;
        var discountAmount = discountType == DiscountType.Percentage
            ? Math.Round(lineTotal * discountValue / 100, 2)
            : discountValue;

        return new OrderItemBase
        {
            Id = EntityId.New<OrderItemId>(), OrderId = orderId,
            ProductId = productId, ProductName = productName, ProductSKU = productSKU,
            Quantity = quantity, UnitPrice = unitPrice, TaxRate = taxRate,
            DiscountType = discountType, DiscountValue = discountValue,
            LineTotal = lineTotal,
            TaxAmount = Math.Round((lineTotal - discountAmount) * taxRate / 100, 2),
            DiscountAmount = discountAmount
        };
    }
}
