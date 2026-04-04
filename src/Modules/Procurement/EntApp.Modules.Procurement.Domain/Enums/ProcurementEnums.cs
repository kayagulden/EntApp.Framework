namespace EntApp.Modules.Procurement.Domain.Enums;

/// <summary>Satın alma talebi durumu.</summary>
public enum PurchaseRequestStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Ordered = 4,
    Cancelled = 5
}

/// <summary>Satın alma siparişi durumu.</summary>
public enum PurchaseOrderStatus
{
    Draft = 0,
    Sent = 1,
    Confirmed = 2,
    PartiallyReceived = 3,
    Received = 4,
    Invoiced = 5,
    Closed = 6,
    Cancelled = 7
}

/// <summary>Tedarikçi değerlendirme.</summary>
public enum SupplierRating
{
    Unrated = 0,
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}

/// <summary>3-way matching durumu.</summary>
public enum MatchingStatus
{
    NotMatched = 0,
    PartialMatch = 1,
    FullMatch = 2,
    Mismatch = 3
}
