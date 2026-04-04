namespace EntApp.Modules.Sales.Domain.Enums;

/// <summary>Sipariş durumu.</summary>
public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Returned = 6
}

/// <summary>Fiyat listesi tipi.</summary>
public enum PriceListType
{
    Standard = 0,
    Wholesale = 1,
    Dealer = 2,
    Campaign = 3
}

/// <summary>İskonto tipi.</summary>
public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}
