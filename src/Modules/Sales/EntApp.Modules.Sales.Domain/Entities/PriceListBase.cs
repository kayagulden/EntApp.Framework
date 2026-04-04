using EntApp.Modules.Sales.Domain.Enums;
using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Sales.Domain.Entities;

/// <summary>Fiyat listesi — ürün fiyatlandırma şablonu.</summary>
[DynamicEntity("PriceList", MenuGroup = "Satış")]
public sealed class PriceListBase : AuditableEntity<Guid>, ITenantEntity
{
    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 50, Searchable = true)]
    public string Code { get; private set; } = string.Empty;

    [DynamicField(FieldType = FieldType.String, Required = true, MaxLength = 200, Searchable = true)]
    public string Name { get; private set; } = string.Empty;

    public PriceListType ListType { get; private set; } = PriceListType.Standard;

    [DynamicField(FieldType = FieldType.String, MaxLength = 10)]
    public string Currency { get; private set; } = "TRY";

    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Fiyat kalemleri — JSON</summary>
    public string PriceItemsJson { get; private set; } = "[]";

    public Guid TenantId { get; set; }

    private PriceListBase() { }

    public static PriceListBase Create(string code, string name,
        PriceListType listType = PriceListType.Standard, string currency = "TRY",
        DateTime? validFrom = null, DateTime? validTo = null, string? priceItemsJson = null)
    {
        return new PriceListBase
        {
            Id = Guid.NewGuid(), Code = code, Name = name,
            ListType = listType, Currency = currency,
            ValidFrom = validFrom, ValidTo = validTo,
            PriceItemsJson = priceItemsJson ?? "[]"
        };
    }
}
