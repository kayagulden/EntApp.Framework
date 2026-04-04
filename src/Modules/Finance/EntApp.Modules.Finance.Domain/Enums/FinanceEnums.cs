namespace EntApp.Modules.Finance.Domain.Enums;

/// <summary>Hesap tipi.</summary>
public enum AccountType
{
    Customer = 0,
    Supplier = 1,
    Bank = 2,
    Cash = 3,
    Other = 4
}

/// <summary>Fatura tipi.</summary>
public enum InvoiceType
{
    Sales = 0,
    Purchase = 1,
    CreditNote = 2,
    DebitNote = 3
}

/// <summary>Fatura durumu.</summary>
public enum InvoiceStatus
{
    Draft = 0,
    Approved = 1,
    Sent = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Cancelled = 6
}

/// <summary>Ödeme yöntemi.</summary>
public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    CreditCard = 2,
    Check = 3,
    Other = 4
}

/// <summary>Ödeme yönü.</summary>
public enum PaymentDirection
{
    Incoming = 0,
    Outgoing = 1
}
