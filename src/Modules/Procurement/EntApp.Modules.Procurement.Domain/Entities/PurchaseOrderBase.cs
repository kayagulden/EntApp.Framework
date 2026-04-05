using EntApp.Modules.Procurement.Domain.Enums;
using EntApp.Modules.Procurement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Procurement.Domain.Entities;

/// <summary>Satın alma siparişi — tedarikçiye gönderilir.</summary>
[DynamicEntity("PurchaseOrder", MenuGroup = "Satın Alma")]
public sealed class PurchaseOrderBase : AuditableEntity<PurchaseOrderId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string OrderNumber { get; private set; } = string.Empty;

    public SupplierId SupplierId { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? SupplierName { get; private set; }

    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;

    public DateTime OrderDate { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public DateTime? ActualDeliveryDate { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    /// <summary>Sipariş kalemleri — JSON</summary>
    public string ItemsJson { get; private set; } = "[]";

    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }

    /// <summary>Teslim alınan toplam tutar</summary>
    public decimal ReceivedTotal { get; private set; }

    /// <summary>3-way matching durumu (PO ↔ GRN ↔ Invoice)</summary>
    public MatchingStatus MatchingStatus { get; private set; } = MatchingStatus.NotMatched;

    /// <summary>İlişkili fatura ID (Finance modülü)</summary>
    public Guid? InvoiceId { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 1000)]
    public string? Notes { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public SupplierBase Supplier { get; private set; } = null!;

    private PurchaseOrderBase() { }

    public static PurchaseOrderBase Create(string orderNumber, SupplierId supplierId,
        DateTime orderDate, string? supplierName = null, string currency = "TRY",
        DateTime? expectedDeliveryDate = null, string? itemsJson = null,
        decimal subTotal = 0, decimal taxTotal = 0, string? notes = null)
    {
        return new PurchaseOrderBase
        {
            Id = EntityId.New<PurchaseOrderId>(), OrderNumber = orderNumber,
            SupplierId = supplierId, SupplierName = supplierName,
            OrderDate = orderDate, Currency = currency,
            ExpectedDeliveryDate = expectedDeliveryDate,
            ItemsJson = itemsJson ?? "[]",
            SubTotal = subTotal, TaxTotal = taxTotal,
            GrandTotal = subTotal + taxTotal,
            Notes = notes
        };
    }

    public void Send() => Status = PurchaseOrderStatus.Sent;
    public void Confirm() => Status = PurchaseOrderStatus.Confirmed;

    public void ReceivePartial(decimal amount)
    {
        ReceivedTotal += amount;
        Status = PurchaseOrderStatus.PartiallyReceived;
    }

    public void ReceiveFull()
    {
        ReceivedTotal = GrandTotal;
        Status = PurchaseOrderStatus.Received;
        ActualDeliveryDate = DateTime.UtcNow;
    }

    public void MatchInvoice(Guid invoiceId)
    {
        InvoiceId = invoiceId;
        MatchingStatus = ReceivedTotal == GrandTotal
            ? MatchingStatus.FullMatch : MatchingStatus.PartialMatch;
        Status = PurchaseOrderStatus.Invoiced;
    }

    public void Close() => Status = PurchaseOrderStatus.Closed;
    public void Cancel() => Status = PurchaseOrderStatus.Cancelled;
}
