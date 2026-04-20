using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.TaskManagement.Domain.Entities;

/// <summary>
/// Lisans kaydı — ConfigurationItemBase'den türer.
/// Yazılım lisanslarına özgü alanlar burada tanımlanır.
/// TPT: configuration_items (base) + licences (derived) tabloları.
/// Uygulama/Server ataması CIRelationship (LicensedTo) ile yönetilir.
/// </summary>
[DynamicEntity("Licence", MenuGroup = "CMDB")]
public sealed class LicenceCI : ConfigurationItemBase
{
    public LicenceType LicenceType { get; private set; } = LicenceType.Subscription;

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? Vendor { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 200)]
    public string? ProductName { get; private set; }

    /// <summary>Maskelenmiş lisans anahtarı.</summary>
    [DynamicField(FieldType = FieldType.String, MaxLength = 500)]
    public string? LicenceKey { get; private set; }

    public int? MaxUsers { get; private set; }
    public int? CurrentUsers { get; private set; }

    public DateTime? ExpirationDate { get; private set; }
    public DateTime? PurchaseDate { get; private set; }

    public decimal? AnnualCost { get; private set; }

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string? Currency { get; private set; }

    private LicenceCI() { }

    public static LicenceCI Create(string name, string code, string? description = null,
        LicenceType licenceType = LicenceType.Subscription,
        CICriticality criticality = CICriticality.Medium,
        Guid? ownerUserId = null,
        string? vendor = null, string? productName = null, string? licenceKey = null,
        int? maxUsers = null, int? currentUsers = null,
        DateTime? expirationDate = null, DateTime? purchaseDate = null,
        decimal? annualCost = null, string? currency = null)
    {
        return new LicenceCI
        {
            Id = EntityId.New<ConfigurationItemId>(),
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            LicenceType = licenceType,
            Criticality = criticality,
            OwnerUserId = ownerUserId,
            Vendor = vendor,
            ProductName = productName,
            LicenceKey = licenceKey,
            MaxUsers = maxUsers,
            CurrentUsers = currentUsers,
            ExpirationDate = expirationDate,
            PurchaseDate = purchaseDate,
            AnnualCost = annualCost,
            Currency = currency
        };
    }

    public void Update(string? name = null, string? description = null,
        LicenceType? licenceType = null, CIStatus? status = null,
        CICriticality? criticality = null, Guid? ownerUserId = null,
        string? vendor = null, string? productName = null, string? licenceKey = null,
        int? maxUsers = null, int? currentUsers = null,
        DateTime? expirationDate = null, DateTime? purchaseDate = null,
        decimal? annualCost = null, string? currency = null)
    {
        UpdateBase(name, description, status, criticality, ownerUserId);
        if (licenceType.HasValue) LicenceType = licenceType.Value;
        if (vendor is not null) Vendor = vendor;
        if (productName is not null) ProductName = productName;
        if (licenceKey is not null) LicenceKey = licenceKey;
        if (maxUsers.HasValue) MaxUsers = maxUsers.Value;
        if (currentUsers.HasValue) CurrentUsers = currentUsers.Value;
        if (expirationDate.HasValue) ExpirationDate = expirationDate.Value;
        if (purchaseDate.HasValue) PurchaseDate = purchaseDate.Value;
        if (annualCost.HasValue) AnnualCost = annualCost.Value;
        if (currency is not null) Currency = currency;
    }
}
