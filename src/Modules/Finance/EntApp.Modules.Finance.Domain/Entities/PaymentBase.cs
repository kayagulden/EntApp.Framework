using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Finance.Domain.Entities;

/// <summary>Ödeme kaydı.</summary>
[DynamicEntity("Payment", MenuGroup = "Finans")]
public sealed class PaymentBase : AuditableEntity<Guid>, ITenantEntity
{
    public Guid AccountId { get; private set; }
    public Guid? InvoiceId { get; private set; }

    public PaymentDirection Direction { get; private set; } = PaymentDirection.Incoming;
    public PaymentMethod Method { get; private set; } = PaymentMethod.BankTransfer;

    public decimal Amount { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    public DateTime PaymentDate { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 100)]
    public string? ReferenceNumber { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Notes { get; private set; }

    public Guid TenantId { get; set; }

    // Navigation
    public AccountBase Account { get; private set; } = null!;
    public InvoiceBase? Invoice { get; private set; }

    private PaymentBase() { }

    public static PaymentBase Create(Guid accountId, decimal amount,
        PaymentDirection direction, PaymentMethod method = PaymentMethod.BankTransfer,
        DateTime? paymentDate = null, Guid? invoiceId = null,
        string currency = "TRY", string? referenceNumber = null, string? notes = null)
    {
        return new PaymentBase
        {
            Id = Guid.NewGuid(), AccountId = accountId, Amount = amount,
            Direction = direction, Method = method,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            InvoiceId = invoiceId, Currency = currency,
            ReferenceNumber = referenceNumber, Notes = notes
        };
    }
}
