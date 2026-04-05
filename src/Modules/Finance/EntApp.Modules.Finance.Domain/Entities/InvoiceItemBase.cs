using EntApp.Modules.Finance.Domain.Ids;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Finance.Domain.Entities;

/// <summary>Fatura kalemi.</summary>
public sealed class InvoiceItemBase : AuditableEntity<InvoiceItemId>, ITenantEntity
{
    public InvoiceId InvoiceId { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; } = 20; // %
    public decimal DiscountRate { get; private set; }

    /// <summary>Hesaplanan: Quantity * UnitPrice</summary>
    public decimal LineTotal { get; private set; }

    /// <summary>Hesaplanan: LineTotal * TaxRate / 100</summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>Hesaplanan: LineTotal * DiscountRate / 100</summary>
    public decimal DiscountAmount { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public InvoiceBase Invoice { get; private set; } = null!;

    private InvoiceItemBase() { }

    public static InvoiceItemBase Create(InvoiceId invoiceId, string description,
        decimal quantity, decimal unitPrice, decimal taxRate = 20, decimal discountRate = 0)
    {
        var lineTotal = quantity * unitPrice;
        return new InvoiceItemBase
        {
            Id = EntityId.New<InvoiceItemId>(), InvoiceId = invoiceId,
            Description = description, Quantity = quantity,
            UnitPrice = unitPrice, TaxRate = taxRate, DiscountRate = discountRate,
            LineTotal = lineTotal,
            TaxAmount = Math.Round(lineTotal * taxRate / 100, 2),
            DiscountAmount = Math.Round(lineTotal * discountRate / 100, 2)
        };
    }
}
