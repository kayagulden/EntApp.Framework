using EntApp.Modules.Inventory.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Inventory.Domain.Entities;

/// <summary>Ürün / malzeme.</summary>
[DynamicEntity("Product", MenuGroup = "Stok")]
public sealed class ProductBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string SKU { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, MaxLength = 50, Searchable = true)]
    public string? Barcode { get; private set; }

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.Text, MaxLength = 2000)]
    public string? Description { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? Category { get; private set; }

    public ProductType ProductType { get; private set; } = ProductType.Physical;
    public UnitOfMeasure Unit { get; private set; } = UnitOfMeasure.Piece;

    public decimal UnitPrice { get; private set; }
    public decimal CostPrice { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    /// <summary>Minimum stok seviyesi</summary>
    public decimal MinStock { get; private set; }

    /// <summary>Maksimum stok seviyesi</summary>
    public decimal MaxStock { get; private set; }

    /// <summary>Yeniden sipariş noktası</summary>
    public decimal ReorderPoint { get; private set; }

    public bool IsActive { get; private set; } = true;
    public Guid TenantId { get; set; }

    private ProductBase() { }

    public static ProductBase Create(string sku, string name, ProductType productType = ProductType.Physical,
        UnitOfMeasure unit = UnitOfMeasure.Piece, string? barcode = null, string? description = null,
        string? category = null, decimal unitPrice = 0, decimal costPrice = 0,
        string currency = "TRY", decimal minStock = 0, decimal maxStock = 0, decimal reorderPoint = 0)
    {
        return new ProductBase
        {
            Id = Guid.NewGuid(), SKU = sku, Name = name,
            ProductType = productType, Unit = unit, Barcode = barcode,
            Description = description, Category = category,
            UnitPrice = unitPrice, CostPrice = costPrice, Currency = currency,
            MinStock = minStock, MaxStock = maxStock, ReorderPoint = reorderPoint
        };
    }

    public void Deactivate() => IsActive = false;
}
