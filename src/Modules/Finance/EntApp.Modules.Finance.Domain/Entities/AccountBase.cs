using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Modules.Finance.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Finance.Domain.Entities;

/// <summary>Cari hesap — müşteri, tedarikçi, banka, kasa.</summary>
[DynamicEntity("Account", MenuGroup = "Finans")]
public sealed class AccountBase : AuditableEntity<AccountId>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string Code { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    public AccountType AccountType { get; private set; } = AccountType.Customer;

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? TaxNumber { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? Email { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 20)]
    public string? Phone { get; private set; }

    [DynamicField(FieldType = FieldType.Text, MaxLength = 500)]
    public string? Address { get; private set; }

    /// <summary>Cari bakiye (hesaplanan)</summary>
    public decimal Balance { get; private set; }

    public bool IsActive { get; private set; } = true;
    public Guid TenantId { get; set; }

    // Navigation
    public ICollection<InvoiceBase> Invoices { get; private set; } = [];
    public ICollection<PaymentBase> Payments { get; private set; } = [];

    private AccountBase() { }

    public static AccountBase Create(string code, string name, AccountType accountType,
        string currency = "TRY", string? taxNumber = null, string? email = null,
        string? phone = null, string? address = null)
    {
        return new AccountBase
        {
            Id = EntityId.New<AccountId>(), Code = code, Name = name,
            AccountType = accountType, Currency = currency,
            TaxNumber = taxNumber, Email = email, Phone = phone, Address = address
        };
    }

    public void UpdateBalance(decimal amount) => Balance += amount;
}
