using EntApp.Modules.Inventory.Domain.Enums;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Inventory.Domain.Entities;

/// <summary>Stok hareketi — giriş, çıkış, transfer, sayım düzeltme.</summary>
public sealed class StockMovementBase : AuditableEntity<Guid>, ITenantEntity
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }

    /// <summary>Transfer hedef depo (sadece Transfer tipinde)</summary>
    public Guid? TargetWarehouseId { get; private set; }

    public MovementType MovementType { get; private set; } = MovementType.StockIn;

    /// <summary>Miktar (pozitif)</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Birim maliyet</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Referans belge (fatura no, sipariş no vb.)</summary>
    public string? ReferenceNumber { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Hareket tarihi</summary>
    public DateTime MovementDate { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public ProductBase Product { get; private set; } = null!;
    public WarehouseBase Warehouse { get; private set; } = null!;
    public WarehouseBase? TargetWarehouse { get; private set; }

    private StockMovementBase() { }

    public static StockMovementBase Create(Guid productId, Guid warehouseId,
        MovementType movementType, decimal quantity, decimal unitCost = 0,
        DateTime? movementDate = null, Guid? targetWarehouseId = null,
        string? referenceNumber = null, string? notes = null)
    {
        return new StockMovementBase
        {
            Id = Guid.NewGuid(), ProductId = productId, WarehouseId = warehouseId,
            MovementType = movementType, Quantity = quantity, UnitCost = unitCost,
            MovementDate = movementDate ?? DateTime.UtcNow,
            TargetWarehouseId = targetWarehouseId,
            ReferenceNumber = referenceNumber, Notes = notes
        };
    }
}
