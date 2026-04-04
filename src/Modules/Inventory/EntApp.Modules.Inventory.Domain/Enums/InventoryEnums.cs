namespace EntApp.Modules.Inventory.Domain.Enums;

/// <summary>Ürün tipi.</summary>
public enum ProductType
{
    Physical = 0,
    Service = 1,
    Digital = 2
}

/// <summary>Birim.</summary>
public enum UnitOfMeasure
{
    Piece = 0,
    Kilogram = 1,
    Liter = 2,
    Meter = 3,
    SquareMeter = 4,
    Box = 5,
    Pallet = 6
}

/// <summary>Stok hareket tipi.</summary>
public enum MovementType
{
    /// <summary>Giriş</summary>
    StockIn = 0,

    /// <summary>Çıkış</summary>
    StockOut = 1,

    /// <summary>Transfer (depolar arası)</summary>
    Transfer = 2,

    /// <summary>Sayım düzeltme</summary>
    Adjustment = 3,

    /// <summary>İade</summary>
    Return = 4
}

/// <summary>Depo durumu.</summary>
public enum WarehouseStatus
{
    Active = 0,
    Inactive = 1,
    Maintenance = 2
}
