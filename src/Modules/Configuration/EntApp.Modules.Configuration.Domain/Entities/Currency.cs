using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Configuration.Domain.Entities;

/// <summary>
/// Para birimi tanımı — ISO 4217 standardında.
/// </summary>
[DynamicEntity("Currency", MenuGroup = "Tanımlar")]
public class Currency : BaseEntity<Guid>
{
    /// <summary>ISO 4217 para birimi kodu (ör: TRY, USD, EUR).</summary>
    [DynamicField(Required = true, MaxLength = 3, Searchable = true)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Para birimi adı.</summary>
    [DynamicField(Required = true, MaxLength = 100, Searchable = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Sembol (ör: ₺, $, €).</summary>
    [DynamicField(MaxLength = 5)]
    public string? Symbol { get; set; }

    /// <summary>Aktif mi?</summary>
    [DynamicField]
    public bool IsActive { get; set; } = true;
}
