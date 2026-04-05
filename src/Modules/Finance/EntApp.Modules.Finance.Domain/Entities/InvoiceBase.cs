using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Modules.Finance.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Finance.Domain.Entities;

/// <summary>Fatura — satış veya satın alma.</summary>
[DynamicEntity("Invoice", MenuGroup = "Finans")]
public sealed class InvoiceBase : AuditableEntity<InvoiceId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string InvoiceNumber { get; private set; } = string.Empty;

    public AccountId AccountId { get; private set; }
    public InvoiceType InvoiceType { get; private set; } = InvoiceType.Sales;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;

    public DateTime InvoiceDate { get; private set; }
    public DateTime DueDate { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public decimal PaidAmount { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 1000)]
    public string? Notes { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public AccountBase Account { get; private set; } = null!;
    public ICollection<InvoiceItemBase> Items { get; private set; } = [];

    private InvoiceBase() { }

    public static InvoiceBase Create(string invoiceNumber, AccountId accountId,
        InvoiceType invoiceType, DateTime invoiceDate, DateTime dueDate,
        string currency = "TRY", string? notes = null)
    {
        return new InvoiceBase
        {
            Id = EntityId.New<InvoiceId>(), InvoiceNumber = invoiceNumber,
            AccountId = accountId, InvoiceType = invoiceType,
            InvoiceDate = invoiceDate, DueDate = dueDate,
            Currency = currency, Notes = notes
        };
    }

    public void Recalculate()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TaxTotal = Items.Sum(i => i.TaxAmount);
        DiscountTotal = Items.Sum(i => i.DiscountAmount);
        GrandTotal = SubTotal + TaxTotal - DiscountTotal;
    }

    public void RecordPayment(decimal amount) => PaidAmount += amount;

    public void Approve() => Status = InvoiceStatus.Approved;
    public void Cancel() => Status = InvoiceStatus.Cancelled;

    public void UpdatePaymentStatus()
    {
        if (PaidAmount >= GrandTotal) Status = InvoiceStatus.Paid;
        else if (PaidAmount > 0) Status = InvoiceStatus.PartiallyPaid;
        else if (DueDate < DateTime.UtcNow && Status == InvoiceStatus.Approved) Status = InvoiceStatus.Overdue;
    }

    public decimal RemainingAmount => GrandTotal - PaidAmount;
}
